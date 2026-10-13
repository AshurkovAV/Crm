using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

/// <summary>
/// Вложение страницы (логический файл). Сами файлы — в WikiAttachmentVersion:
/// «Заменить файл» добавляет новую версию, старые остаются в истории.
/// IsInline = 1 — картинка, вставленная в текст редактора (не показывается в списке вложений).
/// </summary>
public partial class WikiAttachment
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public int PageId { get; set; }

    public bool IsInline { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<WikiAttachmentVersion> Versions { get; set; } = new List<WikiAttachmentVersion>();
}
