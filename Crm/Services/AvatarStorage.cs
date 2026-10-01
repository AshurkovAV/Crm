using System.Text.RegularExpressions;
using Crm.Entity.ModelsCrm;

namespace Crm.Services
{
    /// <summary>
    /// Хранение фото профиля пользователей на диске.
    /// В БД (User.DefaultAvatarId) лежит только имя файла вида "{guid}.jpg",
    /// сами файлы — в папке Avatars:StoragePath (по умолчанию App_Data/avatars),
    /// отдаются через GET /api/avatars/{id}.
    /// </summary>
    public class AvatarStorage
    {
        public const long MaxFileSize = 5 * 1024 * 1024;

        // Имя файла генерируем сами, поэтому принимаем только этот формат —
        // это же защищает от подстановки путей вида "../" в /api/avatars/{id}.
        private static readonly Regex IdPattern = new(@"^[a-f0-9]{32}\.(jpg|png|webp)$", RegexOptions.Compiled);

        private readonly string _directory;

        public AvatarStorage(IWebHostEnvironment environment, IConfiguration configuration)
        {
            var configured = configuration["Avatars:StoragePath"];
            _directory = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(environment.ContentRootPath, "App_Data", "avatars")
                : Path.GetFullPath(configured, environment.ContentRootPath);
            Directory.CreateDirectory(_directory);
        }

        public static bool IsValidId(string? id) => id != null && IdPattern.IsMatch(id);

        public static string? GetUrl(User? user)
        {
            if (user == null || user.IsAvatarEmpty == "true" || !IsValidId(user.DefaultAvatarId))
                return null;
            return $"/api/avatars/{user.DefaultAvatarId}";
        }

        /// <summary>Определяет формат по сигнатуре файла, а не по расширению или Content-Type от клиента.</summary>
        public static string? DetectExtension(ReadOnlySpan<byte> header)
        {
            if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                return "jpg";
            if (header.Length >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
                && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
                return "png";
            if (header.Length >= 12 && header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F'
                && header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P')
                return "webp";
            return null;
        }

        public static string GetContentType(string id) => Path.GetExtension(id) switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        public string? GetPath(string id)
        {
            if (!IsValidId(id))
                return null;
            var path = Path.Combine(_directory, id);
            return File.Exists(path) ? path : null;
        }

        /// <summary>Сохраняет содержимое и возвращает новый id (имя файла).</summary>
        public async Task<string> SaveAsync(byte[] content, string extension)
        {
            var id = $"{Guid.NewGuid():N}.{extension}";
            await File.WriteAllBytesAsync(Path.Combine(_directory, id), content);
            return id;
        }

        public void Delete(string? id)
        {
            if (!IsValidId(id))
                return;
            var path = Path.Combine(_directory, id!);
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
