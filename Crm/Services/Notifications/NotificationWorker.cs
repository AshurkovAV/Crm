using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using Crm.Services.Wiki;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Crm.Services.Notifications
{
    /// <summary>
    /// Отправка очереди уведомлений: повторы с нарастающей паузой (1, 5, 15, 60, 180 минут),
    /// постоянные ошибки — сразу «не отправлено». Статус сохраняется после каждой отправки
    /// (доставка «хотя бы один раз»). Рассчитан на один экземпляр приложения.
    /// </summary>
    public sealed class NotificationDispatcher
    {
        public static readonly TimeSpan[] Backoff =
        {
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromHours(1), TimeSpan.FromHours(3),
        };

        private readonly Dictionary<string, IOutboxChannel> _channels;
        private readonly NotificationOptions _options;
        private readonly ILogger<NotificationDispatcher> _log;

        public NotificationDispatcher(IEnumerable<IOutboxChannel> channels, IOptions<NotificationOptions> options, ILogger<NotificationDispatcher> log)
        {
            _channels = channels.GroupBy(c => c.Channel).ToDictionary(g => g.Key, g => g.Last());
            _options = options.Value;
            _log = log;
        }

        public sealed record Result(int Sent, int Retried, int Failed);

        public async Task<Result> DispatchAsync(DateTime now, CancellationToken ct = default)
        {
            await using var db = new CrmContext();
            var due = await db.NotificationOutbox
                .Where(n => n.Status == NotificationOutbox.StatusPending && n.NextAttemptAt <= now)
                .OrderBy(n => n.NextAttemptAt).ThenBy(n => n.Id)
                .Take(Math.Max(1, _options.BatchSize))
                .ToListAsync(ct);

            int sent = 0, retried = 0, failed = 0;
            foreach (var n in due)
            {
                ct.ThrowIfCancellationRequested();
                SendOutcome outcome;
                if (!_channels.TryGetValue(n.Channel, out var channel))
                {
                    outcome = SendOutcome.Fail("Неизвестный канал " + n.Channel);
                }
                else
                {
                    try
                    {
                        outcome = await channel.SendAsync(n, ct);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _log.LogWarning(ex, "Уведомление {Id}: ошибка канала {Channel}", n.Id, n.Channel);
                        outcome = SendOutcome.Retry(ex.GetType().Name + ": " + ex.Message);
                    }
                }

                n.Attempts++;
                if (outcome.Ok)
                {
                    n.Status = NotificationOutbox.StatusSent;
                    n.SentAt = DateTime.UtcNow;
                    n.LastError = null;
                    sent++;
                }
                else if (outcome.Permanent || n.Attempts >= Math.Max(1, _options.MaxAttempts))
                {
                    n.Status = NotificationOutbox.StatusFailed;
                    n.LastError = Trim(outcome.Error);
                    failed++;
                }
                else
                {
                    var pause = Backoff[Math.Min(n.Attempts - 1, Backoff.Length - 1)];
                    if (outcome.RetryAfter is TimeSpan after && after > pause)
                        pause = after;
                    n.NextAttemptAt = now + pause;
                    n.LastError = Trim(outcome.Error);
                    retried++;
                }

                _log.LogInformation("Уведомление {Id} ({Kind}, {Channel}) пользователю {UserId}: {Status}, попытка {Attempt}{Error}",
                    n.Id, n.Kind, n.Channel, n.UserId, n.Status, n.Attempts, n.LastError == null ? "" : " — " + n.LastError);
                await db.SaveChangesAsync(ct);
            }
            return new Result(sent, retried, failed);
        }

        private static string? Trim(string? s) => s is null ? null : s.Length > 500 ? s[..500] : s;
    }

    /// <summary>
    /// Новые страницы базы знаний → уведомления читателям раздела. События компании копятся, пока идёт
    /// работа (WikiDigestQuietMinutes тишины, но не дольше WikiDigestMaxWaitMinutes), потом каждый читатель
    /// получает одно сообщение со всеми доступными ему страницами. Доступ проверяется в момент рассылки.
    /// </summary>
    public sealed class WikiDigestBuilder
    {
        private readonly INotificationQueue _queue;
        private readonly NotificationOptions _options;
        private readonly ILogger<WikiDigestBuilder> _log;

        public WikiDigestBuilder(INotificationQueue queue, IOptions<NotificationOptions> options, ILogger<WikiDigestBuilder> log)
        {
            _queue = queue;
            _options = options.Value;
            _log = log;
        }

        public async Task<int> RunAsync(DateTime now, bool force = false, CancellationToken ct = default)
        {
            await using var db = new CrmContext();
            var pending = await db.WikiNotificationEvents.Where(e => e.ProcessedAt == null).ToListAsync(ct);
            var quiet = TimeSpan.FromMinutes(Math.Max(0, _options.WikiDigestQuietMinutes));
            var maxWait = TimeSpan.FromMinutes(Math.Max(1, _options.WikiDigestMaxWaitMinutes));
            var queued = 0;

            foreach (var group in pending.GroupBy(e => e.CompanyId))
            {
                var ready = force
                            || group.Max(e => e.CreatedAt) <= now - quiet
                            || group.Min(e => e.CreatedAt) <= now - maxWait;
                if (!ready)
                    continue;

                try
                {
                    queued += await FlushCompanyAsync(db, group.Key, group.ToList(), ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _log.LogError(ex, "База знаний: не удалось разослать уведомления компании {CompanyId}", group.Key);
                }

                foreach (var e in group)
                    e.ProcessedAt = now;
                await db.SaveChangesAsync(ct);
            }
            return queued;
        }

        /// <summary>Анонс страницы: первый «настоящий» абзац (не пункт чек-листа и не короткий заголовок).</summary>
        internal static string? Excerpt(string? contentText)
        {
            var lines = (contentText ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(l => !l.StartsWith("[x]") && !l.StartsWith("[ ]") && !l.StartsWith("[Изображение") && l != "[Видео]")
                .ToList();
            return lines.FirstOrDefault(l => l.Length >= 40) ?? lines.FirstOrDefault();
        }

        private async Task<int> FlushCompanyAsync(CrmContext db, int companyId, List<WikiNotificationEvent> events, CancellationToken ct)
        {
            var pageIds = events.Select(e => e.PageId).Distinct().ToList();
            var pages = await db.WikiPages.AsNoTracking()
                .Where(p => pageIds.Contains(p.Id) && p.CompanyId == companyId && !p.IsDeleted)
                .Select(p => new { p.Id, p.Title, p.SectionId, p.ContentText, SectionName = p.Section.Name, p.Section.AccessMode })
                .ToListAsync(ct);
            if (pages.Count == 0)
                return 0;

            var companyName = await db.Companies.Where(c => c.Id == companyId).Select(c => c.Name).FirstOrDefaultAsync(ct) ?? "";
            var members = await NotificationDirectory.CompanyMembersAsync(db, companyId, ct);
            var sectionIds = pages.Select(p => p.SectionId).Distinct().ToList();
            var granted = await db.WikiSectionAccesses.Where(a => sectionIds.Contains(a.SectionId)).Select(a => new { a.SectionId, a.UserId }).ToListAsync(ct);
            var editors = await db.WikiEditors.Where(e => e.CompanyId == companyId && e.SectionId != null && sectionIds.Contains(e.SectionId.Value))
                .Select(e => new { SectionId = e.SectionId!.Value, e.UserId }).ToListAsync(ct);
            var authorIds = events.Select(e => e.AuthorUserId).Distinct().ToList();
            var authorNames = await db.Users.Where(u => authorIds.Contains(u.Id))
                .Select(u => new { u.Id, u.DisplayName, u.FirstName, u.LastName, u.DefaultEmail }).ToListAsync(ct);
            string NameOf(int id)
            {
                var u = authorNames.FirstOrDefault(x => x.Id == id);
                if (u == null) return "Коллега";
                if (!string.IsNullOrWhiteSpace(u.DisplayName)) return u.DisplayName.Trim();
                var full = $"{u.FirstName} {u.LastName}".Trim();
                return full.Length > 0 ? full : u.DefaultEmail ?? "Коллега";
            }

            // Кому какие страницы (последнее событие по странице определяет «новая/обновлена» и автора)
            var perUser = new Dictionary<int, List<WikiDigestItem>>();
            foreach (var page in pages)
            {
                var pageEvents = events.Where(e => e.PageId == page.Id).OrderBy(e => e.CreatedAt).ToList();
                var last = pageEvents.Last();
                var isUpdate = pageEvents.All(e => e.Kind == "Updated");
                var item = new WikiDigestItem(page.Id, page.Title, page.SectionName, NameOf(last.AuthorUserId), isUpdate,
                    Excerpt(page.ContentText));

                var readers = NotificationRules.SectionReaders(
                    companyId, page.SectionId, page.AccessMode, members,
                    granted.Where(g => g.SectionId == page.SectionId).Select(g => g.UserId).ToHashSet(),
                    editors.Where(e => e.SectionId == page.SectionId).Select(e => e.UserId).ToHashSet(),
                    excludeUserId: null);
                var authors = pageEvents.Select(e => e.AuthorUserId).ToHashSet();
                foreach (var userId in readers.Where(r => !authors.Contains(r)))
                {
                    if (!perUser.TryGetValue(userId, out var list))
                        perUser[userId] = list = new List<WikiDigestItem>();
                    list.Add(item);
                }
            }

            var queued = 0;
            foreach (var (userId, items) in perUser)
            {
                var message = NotificationTemplates.WikiDigest(items, companyName, _queue.BaseUrl);
                queued += await _queue.EnqueueAsync(userId, companyId, message, ct);
            }
            _log.LogInformation("База знаний компании {CompanyId}: {Pages} стр. → {Users} получателей, {Rows} сообщений в очереди",
                companyId, pages.Count, perUser.Count, queued);
            return queued;
        }
    }

    /// <summary>Фоновый воркер: раз в PollSeconds собирает дайджесты базы знаний и отправляет очередь.</summary>
    public sealed class NotificationWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly NotificationOptions _options;
        private readonly ILogger<NotificationWorker> _log;

        public NotificationWorker(IServiceScopeFactory scopes, IOptions<NotificationOptions> options, ILogger<NotificationWorker> log)
        {
            _scopes = scopes;
            _options = options.Value;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var period = TimeSpan.FromSeconds(Math.Clamp(_options.PollSeconds, 2, 600));
            try { await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken); } catch (OperationCanceledException) { return; }
            using var timer = new PeriodicTimer(period);
            do
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<WikiDigestBuilder>().RunAsync(DateTime.UtcNow, ct: stoppingToken);
                    await scope.ServiceProvider.GetRequiredService<NotificationDispatcher>().DispatchAsync(DateTime.UtcNow, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _log.LogError(ex, "Проход уведомлений не удался");
                }
            }
            while (await WaitAsync(timer, stoppingToken));
        }

        private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken ct)
        {
            try { return await timer.WaitForNextTickAsync(ct); }
            catch (OperationCanceledException) { return false; }
        }
    }
}
