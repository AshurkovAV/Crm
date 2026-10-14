using Microsoft.Extensions.Configuration;

namespace Crm.Application.Features.Email.Services
{
    /// <summary>
    /// Настройки почтового сервера — секция <c>Notifications:Smtp</c> (appsettings.Production.json на сервере
    /// или переменные окружения Notifications__Smtp__*). В репозитории логина и пароля нет.
    /// </summary>
    public sealed class SmtpSettings
    {
        public const string Section = "Notifications:Smtp";

        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public string User { get; set; } = "";
        public string Password { get; set; } = "";
        public string From { get; set; } = "";
        public string FromName { get; set; } = "CRM";
        public bool EnableSsl { get; set; } = true;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(From);

        public static SmtpSettings Read(IConfiguration configuration)
        {
            var settings = new SmtpSettings();
            configuration.GetSection(Section).Bind(settings);
            return settings;
        }
    }
}
