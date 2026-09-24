using System;

namespace Crm.Entity.ModelsCrm;

/// <summary>
/// Кому, кроме автора, открыт доступ к записи VaultEntry.
/// </summary>
public partial class VaultEntryAccess
{
    public int Id { get; set; }

    public int VaultEntryId { get; set; }

    public int UserId { get; set; }

    public DateTime GrantedDate { get; set; }

    public virtual VaultEntry VaultEntry { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
