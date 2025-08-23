using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

public partial class ProductSpecification
{
    public int SpecificationId { get; set; }

    public int ProductId { get; set; }

    public int MaterialId { get; set; }

    public decimal Quantity { get; set; }

    public string? Notes { get; set; }

    public virtual Material Material { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
