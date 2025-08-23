using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

public partial class ProductionOrder
{
    public int ProductionOrderId { get; set; }

    public int OrderDetailId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? Status { get; set; }

    public int? ResponsibleEmployeeId { get; set; }

    public string? Notes { get; set; }

    public virtual ICollection<MaterialUsage> MaterialUsages { get; set; } = new List<MaterialUsage>();

    public virtual OrderDetail OrderDetail { get; set; } = null!;

    public virtual Employee? ResponsibleEmployee { get; set; }
}
