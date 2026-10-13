using System;

namespace Crm.Entity.ModelsCrm;

/// <summary>Версия страницы: создаётся при каждом сохранении (снимок заголовка и содержимого).</summary>
public partial class WikiPageVersion
{
    public int Id { get; set; }

    public int PageId { get; set; }

    public int VersionNumber { get; set; }

    public string Title { get; set; } = null!;

    public string? ContentJson { get; set; }

    public string? ContentHtml { get; set; }

    public string? ContentText { get; set; }

    public string? Comment { get; set; }

    public int AuthorUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual WikiPage Page { get; set; } = null!;
}
