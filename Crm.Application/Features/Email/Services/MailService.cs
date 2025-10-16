using Crm.Core.Features.Email.Interfaces;
using Crm.Core.Features.Email.Models;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace Crm.Application.Features.Email.Services
{
    public class MailService : IMailService
    {
        private readonly ILogger<MailService> _logger;
        private readonly string _baseUrl;
        private readonly IConfiguration _configuration;

        public MailService(ILogger<MailService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _baseUrl = configuration["BaseUrl"] ?? "https://crm.biglv.ru";
        }
        public async Task<bool> SendVerificationEmailAsync(string email, string verificationToken)
        {
            try
            {
                var setPasswordUrl = $"{_baseUrl}/Email/SetPassword?token={verificationToken}&email={email}";

                var mail = new Mail
                {
                    EmailFrom = "ashurkovav@yandex.ru",
                    EmailTo = email,
                    EmailSubject = "Завершение регистрации в Crm.System",
                    EmailBody = $@"
                <h2>Завершение регистрации</h2>
                <p>Для завершения регистрации в Crm.System установите ваш пароль:</p>
                <a href='{setPasswordUrl}' style='display: inline-block; padding: 12px 24px; background-color: #7c4dff; color: white; text-decoration: none; border-radius: 8px; font-weight: bold;'>
                    Установить пароль
                </a>
                <p>Или скопируйте ссылку в браузер:</p>
                <p>{setPasswordUrl}</p>
                <p>Ссылка действительна в течение 24 часов.</p>
            "
                };

                return await SendEmailAsync(mail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке верификационного email на {Email}", email);
                return false;
            }
        }

        public async Task<bool> SendEmailAsync(Mail mail)
        {
            try
            {
                var from = new MailAddress(mail.EmailFrom, "Crm.System");
                var to = new MailAddress(mail.EmailTo, "client");

                using var message = new MailMessage(from, to)
                {
                    Subject = mail.EmailSubject,
                    Body = mail.EmailBody,
                    IsBodyHtml = true
                };

                using var smtpClient = new SmtpClient("smtp.yandex.ru", 587)
                {
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(from.Address, "kfoavuebmrejiwzr")
                };

                await smtpClient.SendMailAsync(message);

                _logger.LogInformation("Email отправлен успешно на {EmailTo}", mail.EmailTo);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке email на {EmailTo}", mail.EmailTo);
                return false;
            }
        }
    }
}
