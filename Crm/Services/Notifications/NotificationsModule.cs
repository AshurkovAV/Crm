using Crm.Services.Notifications.Max;
using Microsoft.Extensions.Options;

namespace Crm.Services.Notifications
{
    public static class NotificationsModule
    {
        /// <summary>Флаг командной строки: подписать бота MAX на вебхук и выйти (<c>dotnet Crm.dll --max-register-webhook</c>).</summary>
        public const string RegisterWebhookFlag = "--max-register-webhook";

        /// <summary>Типы обновлений, на которые подписывается вебхук.</summary>
        public static readonly string[] UpdateTypes = { "bot_started", "bot_stopped", "message_created" };

        public const string WebhookPath = "/notifications/max/webhook";

        public static IServiceCollection AddCrmNotifications(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<NotificationOptions>(config.GetSection(NotificationOptions.Section));
            services.AddMemoryCache();
            services.AddHttpClient<MaxBotClient>(MaxBotClient.HttpClientName, (sp, http) =>
            {
                var o = sp.GetRequiredService<IOptions<NotificationOptions>>().Value.Max;
                http.Timeout = TimeSpan.FromSeconds(Math.Clamp(o.TimeoutSeconds, 3, 120));
            });
            services.AddSingleton<INotificationQueue, NotificationQueue>();
            services.AddTransient<IOutboxChannel, EmailOutboxChannel>();
            services.AddTransient<IOutboxChannel, MaxOutboxChannel>();
            services.AddScoped<NotificationDispatcher>();
            services.AddScoped<WikiDigestBuilder>();
            services.AddScoped<MaxLinkService>();

            var enabled = config.GetValue<bool?>($"{NotificationOptions.Section}:{nameof(NotificationOptions.WorkerEnabled)}") ?? true;
            if (enabled)
                services.AddHostedService<NotificationWorker>();
            return services;
        }

        /// <summary>
        /// POST /subscriptions с адресом {BaseUrl}/notifications/max/webhook и секретом из Notifications:Max:WebhookSecret.
        /// Возвращает код выхода процесса.
        /// </summary>
        public static async Task<int> RegisterMaxWebhookAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;
            var options = sp.GetRequiredService<IOptions<NotificationOptions>>().Value;
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Crm.Notifications");
            if (!options.Max.IsConfigured) { log.LogError("Не задан Notifications:Max:BotToken"); return 2; }
            if (string.IsNullOrWhiteSpace(options.Max.WebhookSecret)) { log.LogError("Не задан Notifications:Max:WebhookSecret"); return 2; }
            var client = sp.GetRequiredService<MaxBotClient>();
            var me = await client.GetMeAsync();
            if (me is null) { log.LogError("GET /me не ответил — проверьте токен и Notifications:Max:ApiBaseUrl"); return 3; }
            var url = sp.GetRequiredService<INotificationQueue>().BaseUrl + WebhookPath;
            var r = await client.SubscribeAsync(url, UpdateTypes, options.Max.WebhookSecret);
            if (!r.Ok) { log.LogError("POST /subscriptions: {Status} {Code} {Error}", r.StatusCode, r.ErrorCode, r.Error); return 4; }
            log.LogInformation("Бот @{Bot} подписан на {Url}", me.Username ?? me.Name, url);
            return 0;
        }
    }
}
