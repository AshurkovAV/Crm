using System.Security.Cryptography;
using System.Text;
using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Crm.Services.Notifications.Max
{
    /// <summary>
    /// Привязка пользователя CRM к MAX. Одноразовый токен (в БД — только SHA-256, живёт 15 минут) уходит в ссылку
    /// https://max.ru/&lt;BotName&gt;?start=&lt;token&gt;; пользователь жмёт «Начать», MAX присылает на вебхук bot_started
    /// с payload (или сообщение «/start &lt;token&gt;»), и мы сохраняем его MAX user_id (таблица UserMaxLink).
    /// bot_stopped — привязка снимается. Текст сообщений пользователей не хранится.
    /// </summary>
    public sealed class MaxLinkService
    {
        public static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan DedupWindow = TimeSpan.FromMinutes(30);

        private readonly MaxOptions _max;
        private readonly MaxBotClient _client;
        private readonly IMemoryCache _cache;
        private readonly ILogger<MaxLinkService> _log;

        public MaxLinkService(IOptions<NotificationOptions> options, MaxBotClient client, IMemoryCache cache, ILogger<MaxLinkService> log)
        {
            _max = options.Value.Max;
            _client = client;
            _cache = cache;
            _log = log;
        }

        public bool CanLink => _max.CanLink;

        public string? BotName => string.IsNullOrWhiteSpace(_max.BotName) ? null : _max.BotName.TrimStart('@');

        /// <summary>Новая ссылка привязки (предыдущая перестаёт действовать). null — бот не настроен.</summary>
        public async Task<string?> CreateLinkAsync(int userId, CancellationToken ct = default)
        {
            if (!_max.CanLink)
                return null;
            await using var db = new CrmContext();
            var link = await GetOrCreateAsync(db, userId, ct);
            var token = NewToken();
            link.LinkTokenHash = Hash(token);
            link.LinkTokenExpiresAt = DateTime.UtcNow + TokenLifetime;
            await db.SaveChangesAsync(ct);
            return $"{_max.LinkBaseUrl.TrimEnd('/')}/{Uri.EscapeDataString(_max.BotName.TrimStart('@'))}?start={token}";
        }

        public async Task UnlinkAsync(int userId, CancellationToken ct = default)
        {
            await using var db = new CrmContext();
            var link = await db.UserMaxLinks.FirstOrDefaultAsync(l => l.UserId == userId, ct);
            if (link == null)
                return;
            link.MaxUserId = null;
            link.LinkedAt = null;
            link.LinkTokenHash = null;
            link.LinkTokenExpiresAt = null;
            await db.SaveChangesAsync(ct);
        }

        public enum Outcome { Ignored, Duplicate, Linked, Expired, Hello, Stopped }

        /// <summary>Обработка входящего обновления вебхука. Повтор того же обновления (ретрай MAX) ничего не меняет.</summary>
        public async Task<Outcome> HandleAsync(MaxUpdate update, CancellationToken ct = default)
        {
            var (maxUserId, payload, isStart) = Extract(update);
            if (maxUserId is null)
                return Outcome.Ignored;

            var key = $"max-upd:{update.UpdateType}:{update.Timestamp}:{maxUserId}:{update.Message?.Body?.Mid}";
            if (_cache.TryGetValue(key, out _))
                return Outcome.Duplicate;
            _cache.Set(key, true, DedupWindow);

            await using var db = new CrmContext();
            if (update.UpdateType == "bot_stopped")
            {
                // Пользователь остановил бота — писать ему бесполезно, снимаем привязку.
                var linked = await db.UserMaxLinks.Where(l => l.MaxUserId == maxUserId).ToListAsync(ct);
                foreach (var l in linked)
                {
                    l.MaxUserId = null;
                    l.LinkedAt = null;
                }
                if (linked.Count > 0)
                    await db.SaveChangesAsync(ct);
                return Outcome.Stopped;
            }

            if (!isStart)
                return Outcome.Ignored;
            if (string.IsNullOrWhiteSpace(payload))
            {
                await _client.SendMessageAsync(maxUserId.Value, NotificationTemplates.HelloReply, ct: ct);
                return Outcome.Hello;
            }

            var hash = Hash(payload.Trim());
            var now = DateTime.UtcNow;
            var link = await db.UserMaxLinks.FirstOrDefaultAsync(l => l.LinkTokenHash == hash, ct);
            if (link == null)
            {
                // Тот же токен второй раз (bot_started и «/start …» от одного нажатия) — уже подключено, молчим.
                var since = now - TokenLifetime;
                if (await db.UserMaxLinks.AnyAsync(l => l.MaxUserId == maxUserId && l.LinkedAt > since, ct))
                    return Outcome.Duplicate;
            }

            var userActive = link != null && await db.Users.AnyAsync(u => u.Id == link.UserId && u.IsActive, ct);
            if (link == null || link.LinkTokenExpiresAt is null || link.LinkTokenExpiresAt <= now || !userActive)
            {
                if (link != null)
                {
                    link.LinkTokenHash = null;
                    link.LinkTokenExpiresAt = null;
                    await db.SaveChangesAsync(ct);
                }
                await _client.SendMessageAsync(maxUserId.Value, NotificationTemplates.ExpiredReply, ct: ct);
                return Outcome.Expired;
            }

            // Один MAX-аккаунт — одна учётная запись CRM: старые привязки этого MAX снимаем
            var others = await db.UserMaxLinks.Where(l => l.MaxUserId == maxUserId && l.UserId != link.UserId).ToListAsync(ct);
            foreach (var o in others)
            {
                o.MaxUserId = null;
                o.LinkedAt = null;
            }

            link.MaxUserId = maxUserId;
            link.LinkedAt = now;
            link.LinkTokenHash = null; // одноразовый
            link.LinkTokenExpiresAt = null;
            await db.SaveChangesAsync(ct);
            _log.LogInformation("MAX подключён к пользователю {UserId}", link.UserId);
            await _client.SendMessageAsync(maxUserId.Value, NotificationTemplates.LinkedReply, ct: ct);
            return Outcome.Linked;
        }

        /// <summary>Кто и с каким токеном: bot_started (payload) или сообщение «/start &lt;token&gt;».</summary>
        internal static (long? MaxUserId, string? Payload, bool IsStart) Extract(MaxUpdate u)
        {
            switch (u.UpdateType)
            {
                case "bot_started":
                    return (u.User?.UserId, u.Payload, true);
                case "bot_stopped":
                    return (u.User?.UserId, null, false);
                case "message_created":
                    var sender = u.Message?.Sender;
                    if (sender is null || sender.IsBot)
                        return (null, null, false);
                    var text = u.Message?.Body?.Text?.Trim() ?? "";
                    if (!text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
                        return (sender.UserId, null, false);
                    var rest = text.Length > 6 && char.IsWhiteSpace(text[6]) ? text[7..].Trim() : text.Length == 6 ? "" : null;
                    return rest is null ? (sender.UserId, null, false) : (sender.UserId, rest, true);
                default:
                    return (null, null, false);
            }
        }

        private static async Task<UserMaxLink> GetOrCreateAsync(CrmContext db, int userId, CancellationToken ct)
        {
            var link = await db.UserMaxLinks.FirstOrDefaultAsync(l => l.UserId == userId, ct);
            if (link == null)
            {
                link = new UserMaxLink { UserId = userId };
                db.UserMaxLinks.Add(link);
            }
            return link;
        }

        internal static string NewToken()
        {
            // 24 байта → 32 символа base64url: годится для параметра start (латиница, цифры, «-», «_»).
            var bytes = RandomNumberGenerator.GetBytes(24);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        internal static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    }
}
