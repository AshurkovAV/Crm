using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

public partial class Shipment
{
    public int ShipmentId { get; set; }

    public int OrderId { get; set; }

    public DateTime? ShipmentDate { get; set; }

    public string? Carrier { get; set; }

    public string? TrackingNumber { get; set; }

    public string? Notes { get; set; }

    public int? EmployeeId { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual CustomerOrder Order { get; set; } = null!;

    public virtual ICollection<ShipmentDetail> ShipmentDetails { get; set; } = new List<ShipmentDetail>();
}
