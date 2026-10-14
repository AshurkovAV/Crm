using Microsoft.Extensions.FileProviders;

namespace Crm.Services
{
    /// <summary>
    /// Где лежат файлы, которые загружают пользователи (картинки задач, фото производственных заданий,
    /// аватары компаний и профилей, вложения базы знаний).
    ///
    /// Если задан <c>Storage:Root</c> (например <c>C:\sites\crm-data</c>) — всё хранится там, вне папки публикации,
    /// и публикация с очисткой папки сайта файлы не трогает:
    ///   {Root}\uploads\tasks | production | avatars — отдаются по прежним адресам /uploads/...;
    ///   {Root}\wiki, {Root}\avatars — вложения базы знаний и фото профилей.
    /// Если не задан — как раньше: wwwroot\uploads и App_Data внутри папки сайта.
    /// Явные Wiki:StoragePath / Avatars:StoragePath по-прежнему имеют приоритет.
    /// </summary>
    public sealed class StoragePaths
    {
        public StoragePaths(IWebHostEnvironment environment, IConfiguration configuration)
        {
            var root = configuration["Storage:Root"];
            Root = string.IsNullOrWhiteSpace(root) ? null : Path.GetFullPath(root, environment.ContentRootPath);

            LegacyUploadsDirectory = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads");
            UploadsDirectory = Root != null ? Path.Combine(Root, "uploads") : LegacyUploadsDirectory;

            LegacyAppDataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
            WikiDirectory = Resolve(configuration["Wiki:StoragePath"], environment, Root != null ? Path.Combine(Root, "wiki") : Path.Combine(LegacyAppDataDirectory, "wiki"));
            AvatarsDirectory = Resolve(configuration["Avatars:StoragePath"], environment, Root != null ? Path.Combine(Root, "avatars") : Path.Combine(LegacyAppDataDirectory, "avatars"));
        }

        /// <summary>Корень хранилища вне папки сайта или null, если не настроен.</summary>
        public string? Root { get; }

        /// <summary>Папка, которая отдаётся по адресу /uploads.</summary>
        public string UploadsDirectory { get; }

        public string WikiDirectory { get; }

        public string AvatarsDirectory { get; }

        internal string LegacyUploadsDirectory { get; }

        internal string LegacyAppDataDirectory { get; }

        /// <summary>Абсолютная папка внутри /uploads, например Uploads("tasks") или Uploads("production", "15").</summary>
        public string Uploads(params string[] parts)
        {
            var path = Path.Combine(new[] { UploadsDirectory }.Concat(parts).ToArray());
            Directory.CreateDirectory(path);
            return path;
        }

        private static string Resolve(string? configured, IWebHostEnvironment environment, string fallback) =>
            string.IsNullOrWhiteSpace(configured) ? fallback : Path.GetFullPath(configured, environment.ContentRootPath);

        /// <summary>Отдаёт /uploads из внешней папки (если она настроена). Вызывать до app.UseStaticFiles().</summary>
        public void UseExternalUploads(IApplicationBuilder app)
        {
            if (Root == null) return;
            Directory.CreateDirectory(UploadsDirectory);
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(UploadsDirectory),
                RequestPath = "/uploads",
            });
        }

        /// <summary>
        /// Один раз при старте: если хранилище вынесено, копирует в него файлы, оставшиеся в папке сайта
        /// (wwwroot\uploads, App_Data\wiki, App_Data\avatars). Существующие файлы не перезаписываются,
        /// исходные не удаляются.
        /// </summary>
        public int CopyLegacyFiles(ILogger logger)
        {
            if (Root == null) return 0;
            var copied = 0;
            copied += CopyTree(LegacyUploadsDirectory, UploadsDirectory, logger);
            copied += CopyTree(Path.Combine(LegacyAppDataDirectory, "wiki"), WikiDirectory, logger);
            copied += CopyTree(Path.Combine(LegacyAppDataDirectory, "avatars"), AvatarsDirectory, logger);
            if (copied > 0)
                logger.LogInformation("Хранилище файлов: перенесено {Count} файлов из папки сайта в {Root}", copied, Root);
            return copied;
        }

        private static int CopyTree(string from, string to, ILogger logger)
        {
            if (!Directory.Exists(from) || string.Equals(Path.GetFullPath(from).TrimEnd('\\', '/'), Path.GetFullPath(to).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase))
                return 0;
            var copied = 0;
            foreach (var file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
            {
                var target = Path.Combine(to, Path.GetRelativePath(from, file));
                if (File.Exists(target)) continue;
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Copy(file, target);
                    copied++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Хранилище файлов: не удалось скопировать {File}", file);
                }
            }
            return copied;
        }
    }
}
