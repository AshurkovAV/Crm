using System;

namespace Crm.Entity.ModelsCrm;

/// <summary>
/// Очередь исходящих уведомлений (outbox). Запись создаётся в запросе пользователя, отправляет её
/// фоновый NotificationWorker с повторами — запрос никогда не ждёт SMTP или MAX.
/// Channel: Email | Max. Status: Pending | Sent | Failed | Skipped.
/// </summary>
public partial class NotificationOutbox
{
    public const string ChannelEmail = "Email";
    public const string ChannelMax = "Max";

    public const string StatusPending = "Pending";
    public const string StatusSent = "Sent";
    public const string StatusFailed = "Failed";
    public const string StatusSkipped = "Skipped";

    public long Id { get; set; }

    public int? CompanyId { get; set; }

    public int UserId { get; set; }

    public string Channel { get; set; } = ChannelEmail;

    /// <summary>TaskAssigned | TaskUpdated | WikiPages | System.</summary>
    public string Kind { get; set; } = null!;

    /// <summary>Тема письма / заголовок.</summary>
    public string Subject { get; set; } = null!;

    /// <summary>HTML письма (для Email).</summary>
    public string? BodyHtml { get; set; }

    /// <summary>Текст: plain-text альтернатива письма или markdown-сообщение MAX.</summary>
    public string BodyText { get; set; } = null!;

    public string? LinkUrl { get; set; }

    public string? LinkText { get; set; }

    public string Status { get; set; } = StatusPending;

    public int Attempts { get; set; }

    public DateTime NextAttemptAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? SentAt { get; set; }

    public string? LastError { get; set; }
}
