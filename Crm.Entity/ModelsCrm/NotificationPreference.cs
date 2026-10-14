using System;

namespace Crm.Entity.ModelsCrm;

/// <summary>Настройки уведомлений пользователя (нет строки — всё включено).</summary>
public partial class NotificationPreference
{
    public int UserId { get; set; }

    public bool TaskEmail { get; set; } = true;

    public bool TaskMax { get; set; } = true;

    public bool WikiEmail { get; set; } = true;

    public bool WikiMax { get; set; } = true;

    public DateTime ModifiedDate { get; set; }
}
