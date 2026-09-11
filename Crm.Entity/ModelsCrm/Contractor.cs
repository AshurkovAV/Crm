using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

/// <summary>
/// Внешний подрядчик (смежник), Модуль В ТЗ. Не имеет логина — доступ по Magic Link.
/// </summary>
public partial class Contractor
{
    public int ContractorId { get; set; }

    public int CompanyId { get; set; }

    public string Name { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<ProductionTask> ProductionTasks { get; set; } = new List<ProductionTask>();
}
