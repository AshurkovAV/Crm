using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

/// <summary>
/// Изделие в составе сделки (OrderMaterial из ТЗ) — то, что реально идёт в производство.
/// </summary>
public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int DealId { get; set; }

    public int? ProductTemplateId { get; set; }

    public string Name { get; set; } = null!;

    public decimal Quantity { get; set; } = 1;

    public decimal? Width { get; set; }

    public decimal? Height { get; set; }

    public decimal? Depth { get; set; }

    public decimal CostPrice { get; set; }

    public decimal? MarginPercent { get; set; }

    public decimal Price { get; set; }

    public string Status { get; set; } = "Новое";

    public DateTime CreatedDate { get; set; }

    public virtual Deal Deal { get; set; } = null!;

    public virtual ProductTemplate? ProductTemplate { get; set; }

    public virtual ICollection<ProductionTask> ProductionTasks { get; set; } = new List<ProductionTask>();
}
