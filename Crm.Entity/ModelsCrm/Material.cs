using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

public partial class Material
{
    public int MaterialId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? UnitOfMeasure { get; set; }

    public decimal? CurrentStock { get; set; }

    public decimal? ReorderLevel { get; set; }

    public decimal? StandardCost { get; set; }

    public int? SupplierId { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<MaterialUsage> MaterialUsages { get; set; } = new List<MaterialUsage>();

    public virtual ICollection<ProductSpecification> ProductSpecifications { get; set; } = new List<ProductSpecification>();

    public virtual ICollection<PurchaseDetail> PurchaseDetails { get; set; } = new List<PurchaseDetail>();
}
