using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

/// <summary>
/// Страница базы знаний. Дерево внутри раздела (ParentId), до 3 уровней.
/// Содержимое хранится как JSON редактора (ContentJson) + очищенный санитайзером HTML
/// (ContentHtml, только он выводится читателям) + плоский текст для поиска (ContentText).
/// Version — номер последней сохранённой версии, по нему ловим конфликт одновременного сохранения.
/// </summary>
public partial class WikiPage
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public int SectionId { get; set; }

    public int? ParentId { get; set; }

    public string Title { get; set; } = null!;

    public string? ContentJson { get; set; }

    public string? ContentHtml { get; set; }

    public string? ContentText { get; set; }

    public int SortOrder { get; set; }

    public bool IsPinned { get; set; }

    public int Version { get; set; }

    /// <summary>Ответственный за актуальность страницы.</summary>
    public int? OwnerUserId { get; set; }

    /// <summary>«Проверить до» — после этой даты страница считается устаревшей.</summary>
    public DateTime? ReviewDueDate { get; set; }

    public DateTime? LastReviewedDate { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public int ModifiedByUserId { get; set; }

    public DateTime ModifiedDate { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedDate { get; set; }

    public virtual WikiSection Section { get; set; } = null!;

    public virtual ICollection<WikiPageVersion> Versions { get; set; } = new List<WikiPageVersion>();
}
