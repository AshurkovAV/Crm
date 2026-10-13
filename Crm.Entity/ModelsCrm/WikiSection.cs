using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

/// <summary>
/// Раздел (пространство) базы знаний компании. Доступ: «Все сотрудники компании»
/// (AccessMode = All) или «Только выбранные» (AccessMode = Selected, список в WikiSectionAccess).
/// </summary>
public partial class WikiSection
{
    public const string AccessAll = "All";
    public const string AccessSelected = "Selected";

    public int Id { get; set; }

    public int CompanyId { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>Эмодзи или короткий значок раздела.</summary>
    public string? Icon { get; set; }

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public string AccessMode { get; set; } = AccessAll;

    public int CreatedByUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime ModifiedDate { get; set; }

    public virtual ICollection<WikiSectionAccess> AccessList { get; set; } = new List<WikiSectionAccess>();

    public virtual ICollection<WikiPage> Pages { get; set; } = new List<WikiPage>();
}
