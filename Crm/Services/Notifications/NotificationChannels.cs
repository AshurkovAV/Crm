using System.Net;
using System.Net.Mail;
using System.Text;
using Crm.Entity.ModelsCrm;
using Crm.Services.Email;
using Crm.Services.Notifications.Max;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Crm.Services.Notifications
{
    /// <summary>Итог попытки отправки. Permanent — повтор не поможет.</summary>
    public sealed record SendOutcome(bool Ok, bool Permanent, string? Error, TimeSpan? RetryAfter = null)
    {
        public static readonly SendOutcome Success = new(true, false, null);
        public static SendOutcome Retry(string? error, TimeSpan? after = null) => new(false, false, error, after);
        public static SendOutcome Fail(string? error) => new(false, true, error);
    }

    /// <summary>Канал доставки записи из outbox.</summary>
    public interface IOutboxChannel
    {
        string Channel { get; }
        Task<SendOutcome> SendAsync(NotificationOutbox item, CancellationToken ct);
    }

    /// <summary>
    /// Письмо. С настройкой Notifications:Smtp — через этот SMTP-сервер; без неё — через почтовый ящик CRM
    /// (EmailService, как приглашения сотрудников). Адрес получателя берётся из профиля в момент отправки.
    /// </summary>
    public sealed class EmailOutboxChannel : IOutboxChannel
    {
        private readonly SmtpOptions _smtp;
        private readonly IServiceScopeFactory _scopes;

        public EmailOutboxChannel(IOptions<NotificationOptions> options, IServiceScopeFactory scopes)
        {
            _smtp = options.Value.Smtp;
            _scopes = scopes;
        }

        public string Channel => NotificationOutbox.ChannelEmail;

        public async Task<SendOutcome> SendAsync(NotificationOutbox item, CancellationToken ct)
        {
            string? to;
            await using (var db = new CrmContext())
                to = await db.Users.Where(u => u.Id == item.UserId && u.IsActive).Select(u => u.DefaultEmail).FirstOrDefaultAsync(ct);
            if (string.IsNullOrWhiteSpace(to))
                return SendOutcome.Fail("У пользователя нет адреса почты");

            if (!_smtp.IsConfigured)
            {
                using var scope = _scopes.CreateScope();
                var legacy = scope.ServiceProvider.GetRequiredService<IEmailService>();
                return await legacy.SendRawAsync(to, item.Subject, item.BodyHtml ?? WebUtility.HtmlEncode(item.BodyText), item.BodyText)
                    ? SendOutcome.Success
                    : SendOutcome.Retry("SMTP: письмо не отправлено (подробности в логе EmailService)");
            }

            try
            {
                using var message = new MailMessage(new MailAddress(_smtp.From, _smtp.FromName), new MailAddress(to))
                {
                    Subject = item.Subject,
                    SubjectEncoding = Encoding.UTF8,
                    BodyEncoding = Encoding.UTF8,
                    Body = item.BodyText,
                    IsBodyHtml = false,
                };
                if (!string.IsNullOrEmpty(item.BodyHtml))
                    message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(item.BodyHtml, Encoding.UTF8, "text/html"));

                using var client = new SmtpClient(_smtp.Host, _smtp.Port) { EnableSsl = _smtp.EnableSsl, Timeout = 20_000 };
                if (!string.IsNullOrEmpty(_smtp.User))
                    client.Credentials = new NetworkCredential(_smtp.User, _smtp.Password);
                await client.SendMailAsync(message, ct);
                return SendOutcome.Success;
            }
            catch (SmtpFailedRecipientException ex) { return SendOutcome.Fail("SMTP: " + ex.StatusCode); }
            catch (SmtpException ex) { return SendOutcome.Retry("SMTP: " + ex.StatusCode + " " + ex.Message); }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or System.Net.Sockets.SocketException)
            {
                return SendOutcome.Retry("SMTP: " + ex.Message);
            }
        }
    }

    /// <summary>Сообщение в MAX: пользователю в личный диалог с ботом, с кнопкой-ссылкой.</summary>
    public sealed class MaxOutboxChannel : IOutboxChannel
    {
        private readonly MaxBotClient _client;

        public MaxOutboxChannel(MaxBotClient client) => _client = client;

        public string Channel => NotificationOutbox.ChannelMax;

        public async Task<SendOutcome> SendAsync(NotificationOutbox item, CancellationToken ct)
        {
            if (!_client.IsConfigured)
                return SendOutcome.Fail("Бот MAX не настроен на сервере");

            long? maxUserId;
            await using (var db = new CrmContext())
                maxUserId = await db.UserMaxLinks.Where(l => l.UserId == item.UserId).Select(l => l.MaxUserId).FirstOrDefaultAsync(ct);
            if (maxUserId is null)
                return SendOutcome.Fail("MAX не подключён");

            var buttons = string.IsNullOrEmpty(item.LinkUrl)
                ? null
                : new[] { new MaxLinkButton(item.LinkText ?? "Открыть", item.LinkUrl) };
            var r = await _client.SendMessageAsync(maxUserId.Value, item.BodyText, buttons, ct);
            if (r.Ok)
                return SendOutcome.Success;
            var error = $"MAX {r.StatusCode} {r.ErrorCode} {r.Error}".Trim();
            return r.Permanent ? SendOutcome.Fail(error) : SendOutcome.Retry(error, r.RetryAfter);
        }
    }
}
