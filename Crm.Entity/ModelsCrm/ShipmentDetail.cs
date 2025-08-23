using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

public partial class ShipmentDetail
{
    public int ShipmentDetailId { get; set; }

    public int ShipmentId { get; set; }

    public int OrderDetailId { get; set; }

    public decimal QuantityShipped { get; set; }

    public string? Notes { get; set; }

    public virtual OrderDetail OrderDetail { get; set; } = null!;

    public virtual Shipment Shipment { get; set; } = null!;
}
