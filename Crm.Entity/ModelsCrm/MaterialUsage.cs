using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

public partial class MaterialUsage
{
    public int UsageId { get; set; }

    public int ProductionOrderId { get; set; }

    public int MaterialId { get; set; }

    public decimal QuantityUsed { get; set; }

    public DateTime? UsageDate { get; set; }

    public string? Notes { get; set; }

    public virtual Material Material { get; set; } = null!;

    public virtual ProductionOrder ProductionOrder { get; set; } = null!;
}
