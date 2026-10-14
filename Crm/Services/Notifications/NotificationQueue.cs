using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Crm.Services.Notifications
{
    /// <summary>Постановка уведомлений в очередь (outbox). Сама отправка — в NotificationWorker.</summary>
    public interface INotificationQueue
    {
        /// <summary>Положить сообщение для пользователя во все каналы, которые он включил. Возвращает число записей.</summary>
        Task<int> EnqueueAsync(int userId, int? companyId, NotificationMessage message, CancellationToken ct = default);

        /// <summary>Отметить новую / существенно обновлённую страницу базы знаний (рассылка — дайджестом).</summary>
        Task AddWikiEventAsync(int companyId, int pageId, int authorUserId, bool isUpdate, CancellationToken ct = default);

        /// <summary>Публичный адрес CRM для ссылок.</summary>
        string BaseUrl { get; }
    }

    public sealed class NotificationQueue : INotificationQueue
    {
        private readonly NotificationOptions _options;
        private readonly string _baseUrl;

        public NotificationQueue(IOptions<NotificationOptions> options, IConfiguration configuration)
        {
            _options = options.Value;
            _baseUrl = ResolveBaseUrl(_options, configuration);
        }

        public string BaseUrl => _baseUrl;

        public static string ResolveBaseUrl(NotificationOptions options, IConfiguration configuration)
        {
            var url = options.BaseUrl;
            if (string.IsNullOrWhiteSpace(url)) url = configuration["BaseUrl"];
            if (string.IsNullOrWhiteSpace(url)) url = "https://crm.biglv.ru";
            return url.TrimEnd('/');
        }

        public async Task<int> EnqueueAsync(int userId, int? companyId, NotificationMessage message, CancellationToken ct = default)
        {
            await using var db = new CrmContext();
            var email = await db.Users.Where(u => u.Id == userId && u.IsActive).Select(u => u.DefaultEmail).FirstOrDefaultAsync(ct);
            var prefs = await db.NotificationPreferences.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);
            var maxLinked = _options.Max.IsConfigured
                            && await db.UserMaxLinks.AnyAsync(l => l.UserId == userId && l.MaxUserId != null, ct);

            var (sendEmail, sendMax) = NotificationRules.Channels(prefs, message.Kind, !string.IsNullOrWhiteSpace(email), maxLinked);
            var now = DateTime.UtcNow;
            var count = 0;
            if (sendEmail)
            {
                db.NotificationOutbox.Add(Row(userId, companyId, NotificationOutbox.ChannelEmail, message, message.Html, message.Text, now));
                count++;
            }
            if (sendMax)
            {
                db.NotificationOutbox.Add(Row(userId, companyId, NotificationOutbox.ChannelMax, message, null, message.MaxText, now));
                count++;
            }
            if (count > 0)
                await db.SaveChangesAsync(ct);
            return count;
        }

        public async Task AddWikiEventAsync(int companyId, int pageId, int authorUserId, bool isUpdate, CancellationToken ct = default)
        {
            await using var db = new CrmContext();
            db.WikiNotificationEvents.Add(new WikiNotificationEvent
            {
                CompanyId = companyId,
                PageId = pageId,
                AuthorUserId = authorUserId,
                Kind = isUpdate ? "Updated" : "Created",
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync(ct);
        }

        private static NotificationOutbox Row(int userId, int? companyId, string channel, NotificationMessage m, string? html, string text, DateTime now) => new()
        {
            UserId = userId,
            CompanyId = companyId,
            Channel = channel,
            Kind = m.Kind,
            Subject = m.Subject.Length > 300 ? m.Subject[..300] : m.Subject,
            BodyHtml = html,
            BodyText = text,
            LinkUrl = m.LinkUrl.Length > 500 ? m.LinkUrl[..500] : m.LinkUrl,
            LinkText = m.LinkText,
            Status = NotificationOutbox.StatusPending,
            NextAttemptAt = now,
            CreatedAt = now,
        };
    }

    /// <summary>Справочные выборки для уведомлений.</summary>
    public static class NotificationDirectory
    {
        /// <summary>Активные участники компании + владелец; руководитель = владелец или роль Admin.</summary>
        public static async Task<List<CompanyMember>> CompanyMembersAsync(CrmContext db, int companyId, CancellationToken ct = default)
        {
            var ownerId = await db.Companies.Where(c => c.Id == companyId).Select(c => (int?)c.OwnerId).FirstOrDefaultAsync(ct);
            var rows = await db.CompanyUsers
                .Where(cu => cu.CompanyId == companyId && cu.IsActive && cu.User.IsActive)
                .Select(cu => new { cu.UserId, cu.Role })
                .ToListAsync(ct);
            var members = rows.GroupBy(r => r.UserId)
                .Select(g => new CompanyMember(g.Key, g.Key == ownerId || g.Any(r => WikiAccess.IsManagerRole(r.Role))))
                .ToList();
            if (ownerId is int owner && members.All(m => m.UserId != owner))
                members.Add(new CompanyMember(owner, true));
            return members;
        }
    }

    /// <summary>
    /// Канал уведомлений о задаче: вместо прямой отправки письма в запросе кладёт сообщение в очередь
    /// (письмо + MAX по настройкам исполнителя). Получатель — по NotificationRules.TaskRecipient.
    /// </summary>
    public sealed class OutboxTaskNotificationChannel : ITaskNotificationChannel
    {
        private readonly INotificationQueue _queue;
        private readonly ILogger<OutboxTaskNotificationChannel> _log;

        public OutboxTaskNotificationChannel(INotificationQueue queue, ILogger<OutboxTaskNotificationChannel> log)
        {
            _queue = queue;
            _log = log;
        }

        public async Task NotifyAsync(TaskNotificationContext context)
        {
            await using var db = new CrmContext();
            var companyId = await db.Users.Where(u => u.Id == context.ActorUserId).Select(u => u.CurrentCompanyId).FirstOrDefaultAsync();
            if (companyId is not int cid)
                return;

            var memberIds = (await NotificationDirectory.CompanyMembersAsync(db, cid)).Select(m => m.UserId).ToHashSet();
            var recipient = NotificationRules.TaskRecipient(context.AssigneeUserId, context.ActorUserId, memberIds);
            if (recipient == null)
                return;

            var message = NotificationTemplates.Task(context, _queue.BaseUrl);
            var queued = await _queue.EnqueueAsync(recipient.Value, cid, message);
            _log.LogInformation("Задача {TaskId}: уведомление исполнителю {UserId} поставлено в очередь ({Count} канал.)", context.TaskId, recipient, queued);
        }
    }
}
