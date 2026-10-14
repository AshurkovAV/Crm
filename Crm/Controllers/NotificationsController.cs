using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Crm.Entity.ModelsCrm;
using Crm.Services.Notifications;
using Crm.Services.Notifications.Max;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Crm.Controllers
{
    /// <summary>
    /// Настройки уведомлений текущего пользователя (карточка «Уведомления» в профиле), привязка MAX
    /// и вебхук бота MAX. Все POST, кроме вебхука, требуют antiforgery-токен.
    /// </summary>
    [Authorize]
    [AutoValidateAntiforgeryToken]
    [Route("notifications")]
    public class NotificationsController : Controller
    {
        public const string SecretHeader = "X-Max-Bot-Api-Secret";
        private const int MaxWebhookBody = 64 * 1024;

        private readonly NotificationOptions _options;
        private readonly MaxLinkService _link;
        private readonly ILogger<NotificationsController> _log;

        public NotificationsController(IOptions<NotificationOptions> options, MaxLinkService link, ILogger<NotificationsController> log)
        {
            _options = options.Value;
            _link = link;
            _log = log;
        }

        [HttpGet("settings")]
        public async Task<IActionResult> Settings()
        {
            var userId = CurrentUserId();
            if (userId == null)
                return Unauthorized();
            return Json(await NotificationSettingsState.LoadAsync(userId.Value, _options));
        }

        public record PreferencesRequest(bool TaskEmail, bool TaskMax, bool WikiEmail, bool WikiMax);

        [HttpPost("preferences")]
        public async Task<IActionResult> SavePreferences([FromBody] PreferencesRequest request)
        {
            var userId = CurrentUserId();
            if (userId == null)
                return Unauthorized();

            await using var db = new CrmContext();
            var prefs = await db.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId.Value);
            if (prefs == null)
            {
                prefs = new NotificationPreference { UserId = userId.Value };
                db.NotificationPreferences.Add(prefs);
            }
            prefs.TaskEmail = request.TaskEmail;
            prefs.TaskMax = request.TaskMax;
            prefs.WikiEmail = request.WikiEmail;
            prefs.WikiMax = request.WikiMax;
            prefs.ModifiedDate = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Ok(new { ok = true });
        }

        /// <summary>Ссылка для подключения MAX (одноразовая, 15 минут).</summary>
        [HttpPost("max/link")]
        public async Task<IActionResult> CreateMaxLink()
        {
            var userId = CurrentUserId();
            if (userId == null)
                return Unauthorized();
            var url = await _link.CreateLinkAsync(userId.Value);
            return url == null
                ? StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Бот MAX ещё не настроен на сервере." })
                : Json(new { Url = url, Minutes = (int)MaxLinkService.TokenLifetime.TotalMinutes });
        }

        [HttpPost("max/unlink")]
        public async Task<IActionResult> UnlinkMax()
        {
            var userId = CurrentUserId();
            if (userId == null)
                return Unauthorized();
            await _link.UnlinkAsync(userId.Value);
            return Ok(new { ok = true });
        }

        /// <summary>
        /// Входящие обновления бота MAX. Секрет — заголовок X-Max-Bot-Api-Secret (его ставит MAX при подписке с secret)
        /// или ?secret= в URL. Без настроенного секрета вебхук выключен (404). Ответ 200 — сразу, повторы безопасны.
        /// </summary>
        [HttpPost("max/webhook"), AllowAnonymous, IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook(CancellationToken ct)
        {
            var secret = _options.Max.WebhookSecret;
            if (string.IsNullOrEmpty(secret) || !_options.Max.IsConfigured)
                return NotFound();
            var given = Request.Headers[SecretHeader].FirstOrDefault() ?? Request.Query["secret"].FirstOrDefault();
            if (given is null || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(given), Encoding.UTF8.GetBytes(secret)))
            {
                _log.LogWarning("Вебхук MAX: неверный секрет с {Ip}", HttpContext.Connection.RemoteIpAddress);
                return Unauthorized();
            }
            if (Request.ContentLength > MaxWebhookBody)
                return StatusCode(StatusCodes.Status413PayloadTooLarge);

            MaxUpdate? update;
            try
            {
                var buffer = new byte[MaxWebhookBody + 1];
                var total = 0;
                int read;
                while (total < buffer.Length && (read = await Request.Body.ReadAsync(buffer.AsMemory(total), ct)) > 0)
                    total += read;
                if (total > MaxWebhookBody)
                    return StatusCode(StatusCodes.Status413PayloadTooLarge);
                update = JsonSerializer.Deserialize<MaxUpdate>(buffer.AsSpan(0, total), MaxBotClient.Json);
            }
            catch (JsonException)
            {
                return BadRequest();
            }
            if (update is null)
                return BadRequest();

            var outcome = await _link.HandleAsync(update, ct);
            _log.LogInformation("Вебхук MAX: {Type} → {Outcome}", update.UpdateType, outcome);
            return Content("OK", "text/plain");
        }

        private int? CurrentUserId()
            => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    }

    /// <summary>Состояние карточки «Уведомления» (для ViewComponent и JSON).</summary>
    public sealed class NotificationSettingsState
    {
        public string? Email { get; init; }
        public bool TaskEmail { get; init; } = true;
        public bool TaskMax { get; init; } = true;
        public bool WikiEmail { get; init; } = true;
        public bool WikiMax { get; init; } = true;
        public bool MaxConfigured { get; init; }
        public bool CanLink { get; init; }
        public bool MaxLinked { get; init; }
        public DateTime? MaxLinkedAt { get; init; }
        public string? BotName { get; init; }

        public static async Task<NotificationSettingsState> LoadAsync(int userId, NotificationOptions options)
        {
            await using var db = new CrmContext();
            var email = await db.Users.Where(u => u.Id == userId).Select(u => u.DefaultEmail).FirstOrDefaultAsync();
            var prefs = await db.NotificationPreferences.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId);
            var link = await db.UserMaxLinks.AsNoTracking().FirstOrDefaultAsync(l => l.UserId == userId);
            return new NotificationSettingsState
            {
                Email = email,
                TaskEmail = prefs?.TaskEmail ?? true,
                TaskMax = prefs?.TaskMax ?? true,
                WikiEmail = prefs?.WikiEmail ?? true,
                WikiMax = prefs?.WikiMax ?? true,
                MaxConfigured = options.Max.IsConfigured,
                CanLink = options.Max.CanLink,
                MaxLinked = link?.MaxUserId != null,
                MaxLinkedAt = link?.LinkedAt,
                BotName = string.IsNullOrWhiteSpace(options.Max.BotName) ? null : options.Max.BotName.TrimStart('@'),
            };
        }
    }
}
