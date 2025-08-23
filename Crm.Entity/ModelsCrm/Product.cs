using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

public partial class Product
{
    public int ProductId { get; set; }

    public int? CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? UnitOfMeasure { get; set; }

    public decimal? StandardCost { get; set; }

    public decimal? SellingPrice { get; set; }

    public int? ProductionTimeDays { get; set; }

    public bool? IsActive { get; set; }

    public virtual ProductCategory? Category { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<ProductSpecification> ProductSpecifications { get; set; } = new List<ProductSpecification>();
}
