using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

/// <summary>
/// Справочник ТМЦ/склада (сырьё, комплектующие). См. ТЗ, сущность Component.
/// </summary>
public partial class Component
{
    public int ComponentId { get; set; }

    public int CompanyId { get; set; }

    public string Name { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public decimal CostPrice { get; set; }

    public decimal StockQuantity { get; set; }

    public decimal? ReorderLevel { get; set; }

    public int? SupplierId { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual Company Company { get; set; } = null!;

    public virtual Supplier? Supplier { get; set; }

    public virtual ICollection<ProductTemplateComponent> ProductTemplateComponents { get; set; } = new List<ProductTemplateComponent>();
}
