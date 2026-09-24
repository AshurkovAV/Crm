using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

/// <summary>
/// Запись в хранилище паролей компании.
/// ВАЖНО: Password хранится в открытом виде, без шифрования — сознательное решение
/// заказчика (не наш вызов), см. обсуждение в задаче на реализацию модуля. Доступ
/// к записи ограничивается только на уровне приложения: автор + пользователи из
/// VaultEntryAccess.
/// </summary>
public partial class VaultEntry
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public string Title { get; set; } = null!;

    public string? Login { get; set; }

    public string? Password { get; set; }

    public string? Url { get; set; }

    public string? Notes { get; set; }

    public string? Tags { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime ModifiedDate { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual ICollection<VaultEntryAccess> AccessList { get; set; } = new List<VaultEntryAccess>();
}
