using System;

namespace Crm.Entity.ModelsCrm;

/// <summary>
/// Событие «появилась / существенно обновлена страница базы знаний». Фоновый воркер собирает события
/// компании за короткое окно и рассылает читателям раздела одно сообщение (дайджест).
/// </summary>
public partial class WikiNotificationEvent
{
    public long Id { get; set; }

    public int CompanyId { get; set; }

    public int PageId { get; set; }

    public int AuthorUserId { get; set; }

    /// <summary>Created | Updated.</summary>
    public string Kind { get; set; } = "Created";

    public DateTime CreatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }
}
