using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

public partial class PurchaseDetail
{
    public int PurchaseDetailId { get; set; }

    public int PurchaseId { get; set; }

    public int MaterialId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? ReceivedQuantity { get; set; }

    public string? Notes { get; set; }

    public virtual Material Material { get; set; } = null!;

    public virtual MaterialPurchase Purchase { get; set; } = null!;
}
