namespace Crm.Services.Notifications
{
    /// <summary>
    /// Настройки уведомлений: секция <c>Notifications</c> в appsettings или переменные окружения
    /// (<c>Notifications__Max__BotToken</c> и т. д.). Секреты (токен бота, пароль SMTP, секрет вебхука)
    /// в репозиторий не кладутся — только на сервере.
    /// </summary>
    public sealed class NotificationOptions
    {
        public const string Section = "Notifications";

        /// <summary>Публичный адрес CRM для ссылок в письмах и MAX (например https://crm.example.ru). Пусто — BaseUrl.</summary>
        public string BaseUrl { get; set; } = "";

        /// <summary>Фоновая отправка. null — включена.</summary>
        public bool? WorkerEnabled { get; set; }

        /// <summary>Как часто воркер смотрит очередь, секунд.</summary>
        public int PollSeconds { get; set; } = 15;

        /// <summary>Сколько попыток отправки, потом — «не отправлено».</summary>
        public int MaxAttempts { get; set; } = 6;

        /// <summary>Сколько сообщений отправлять за один проход.</summary>
        public int BatchSize { get; set; } = 50;

        /// <summary>База знаний: ждать столько минут тишины, чтобы собрать несколько новых страниц в одно сообщение.</summary>
        public int WikiDigestQuietMinutes { get; set; } = 2;

        /// <summary>База знаний: но не дольше стольких минут с первого события.</summary>
        public int WikiDigestMaxWaitMinutes { get; set; } = 15;

        public MaxOptions Max { get; set; } = new();

        public SmtpOptions Smtp { get; set; } = new();
    }

    /// <summary>Бот в мессенджере MAX (создаётся на платформе MAX для бизнеса / через @MasterBot).</summary>
    public sealed class MaxOptions
    {
        /// <summary>Токен бота. Пусто — канал MAX выключен.</summary>
        public string BotToken { get; set; } = "";

        /// <summary>Адрес Bot API.</summary>
        public string ApiBaseUrl { get; set; } = "https://platform-api2.max.ru";

        /// <summary>Имя (username) бота для ссылки https://max.ru/&lt;BotName&gt;?start=… .</summary>
        public string BotName { get; set; } = "";

        /// <summary>Откуда строить ссылку на бота.</summary>
        public string LinkBaseUrl { get; set; } = "https://max.ru";

        /// <summary>Секрет вебхука (заголовок X-Max-Bot-Api-Secret). Пусто — вебхук выключен.</summary>
        public string WebhookSecret { get; set; } = "";

        public int TimeoutSeconds { get; set; } = 15;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(BotToken);

        public bool CanLink => IsConfigured && !string.IsNullOrWhiteSpace(BotName);
    }

    /// <summary>
    /// Почта для уведомлений. Без Host письма уходят через существующий почтовый ящик CRM (EmailService),
    /// как и приглашения сотрудников.
    /// </summary>
    public sealed class SmtpOptions
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public string User { get; set; } = "";
        public string Password { get; set; } = "";
        public string From { get; set; } = "";
        public string FromName { get; set; } = "CRM";
        public bool EnableSsl { get; set; } = true;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(From);
    }
}
