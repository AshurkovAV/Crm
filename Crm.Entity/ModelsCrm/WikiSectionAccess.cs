using System;

namespace Crm.Entity.ModelsCrm;

/// <summary>Кто видит раздел с доступом «Только выбранные».</summary>
public partial class WikiSectionAccess
{
    public int Id { get; set; }

    public int SectionId { get; set; }

    public int UserId { get; set; }

    public DateTime GrantedDate { get; set; }

    public virtual WikiSection Section { get; set; } = null!;
}
