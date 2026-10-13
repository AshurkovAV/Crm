using System.Text.RegularExpressions;
using Microsoft.AspNetCore.StaticFiles;

namespace Crm.Services.Wiki
{
    /// <summary>
    /// Файлы базы знаний на диске: папка Wiki:StoragePath (по умолчанию App_Data/wiki — вне wwwroot,
    /// напрямую по URL недоступна), внутри — подпапка на компанию, имена файлов случайные.
    /// Отдаются только через WikiController.File после проверки доступа к странице.
    /// </summary>
    public class WikiFileStorage
    {
        public const long DefaultMaxFileSizeBytes = 50L * 1024 * 1024;

        private static readonly Regex StoredNamePattern = new(@"^[a-f0-9]{32}(\.[a-z0-9]{1,10})?$", RegexOptions.Compiled);
        private static readonly Regex ExtensionPattern = new(@"^\.[a-z0-9]{1,10}$", RegexOptions.Compiled);
        private static readonly FileExtensionContentTypeProvider ContentTypes = new();

        /// <summary>Типы, которые можно показывать прямо в браузере (картинки и PDF). Всё остальное — только скачивание.</summary>
        private static readonly Dictionary<string, string> InlineTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp",
            [".pdf"] = "application/pdf",
        };

        /// <summary>Активное содержимое: никогда не отдаём с «родным» типом, только как поток байт.</summary>
        private static readonly HashSet<string> DangerousExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".html", ".htm", ".xhtml", ".shtml", ".svg", ".svgz", ".xml", ".xsl", ".xslt", ".js", ".mjs", ".css", ".swf",
        };

        private readonly string _root;

        public WikiFileStorage(IWebHostEnvironment environment, IConfiguration configuration)
        {
            var configured = configuration["Wiki:StoragePath"];
            _root = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(environment.ContentRootPath, "App_Data", "wiki")
                : Path.GetFullPath(configured, environment.ContentRootPath);

            var maxMb = configuration.GetValue<long?>("Wiki:MaxFileSizeMb");
            MaxFileSizeBytes = maxMb is > 0 ? maxMb.Value * 1024 * 1024 : DefaultMaxFileSizeBytes;
        }

        public long MaxFileSizeBytes { get; }

        public static bool IsValidStoredName(string? name) => name != null && StoredNamePattern.IsMatch(name);

        /// <summary>Content-Type по расширению исходного имени (заявленному клиентом типу не доверяем).</summary>
        public static string ResolveContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            if (DangerousExtensions.Contains(extension))
                return "application/octet-stream";
            if (InlineTypes.TryGetValue(extension, out var inline))
                return inline;
            return ContentTypes.TryGetContentType(fileName, out var type) && !type.Contains("html") && !type.Contains("svg")
                ? type
                : "application/octet-stream";
        }

        /// <summary>Можно ли показать файл в браузере (картинка или PDF) — с проверкой сигнатуры содержимого.</summary>
        public static bool CanPreviewInline(string contentType, ReadOnlySpan<byte> header)
            => contentType switch
            {
                "image/png" or "image/jpeg" or "image/webp" => AvatarStorage.DetectExtension(header) != null,
                "image/gif" => header.Length >= 6 && header[0] == 'G' && header[1] == 'I' && header[2] == 'F' && header[3] == '8',
                "application/pdf" => header.Length >= 5 && header[0] == '%' && header[1] == 'P' && header[2] == 'D' && header[3] == 'F' && header[4] == '-',
                _ => false,
            };

        public static bool IsImage(string contentType) => contentType.StartsWith("image/", StringComparison.Ordinal) && contentType != "image/svg+xml";

        /// <summary>Безопасное отображаемое имя файла (без путей и управляющих символов).</summary>
        public static string CleanFileName(string? fileName)
        {
            var name = Path.GetFileName((fileName ?? string.Empty).Replace('\\', '/'));
            name = Regex.Replace(name, @"[\x00-\x1f\x7f""<>|:*?/\\]", "_").Trim();
            if (name.Length == 0)
                name = "file";
            if (name.Length > 200)
            {
                var ext = Path.GetExtension(name);
                name = name[..(200 - Math.Min(ext.Length, 20))] + (ext.Length <= 20 ? ext : "");
            }
            return name;
        }

        /// <summary>Сохраняет поток в папку компании под случайным именем, возвращает это имя.</summary>
        public async Task<string> SaveAsync(int companyId, string originalFileName, Stream content, CancellationToken cancellationToken)
        {
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            if (!ExtensionPattern.IsMatch(extension))
                extension = string.Empty;

            var storedName = $"{Guid.NewGuid():N}{extension}";
            var directory = CompanyDirectory(companyId);
            Directory.CreateDirectory(directory);

            var path = Path.Combine(directory, storedName);
            await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await content.CopyToAsync(file, cancellationToken);
            return storedName;
        }

        public string? GetPath(int companyId, string storedName)
        {
            if (!IsValidStoredName(storedName))
                return null;
            var path = Path.Combine(CompanyDirectory(companyId), storedName);
            return File.Exists(path) ? path : null;
        }

        public void Delete(int companyId, string storedName)
        {
            var path = GetPath(companyId, storedName);
            if (path != null)
                File.Delete(path);
        }

        private string CompanyDirectory(int companyId) => Path.Combine(_root, $"company-{companyId}");
    }
}
