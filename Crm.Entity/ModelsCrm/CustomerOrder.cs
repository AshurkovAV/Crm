using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

public partial class CustomerOrder
{
    public int OrderId { get; set; }

    public int ClientId { get; set; }

    public DateTime? OrderDate { get; set; }

    public DateTime? RequiredDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    public string? Status { get; set; }

    public decimal? TotalAmount { get; set; }

    public decimal? Discount { get; set; }

    public string? Notes { get; set; }

    public int? EmployeeId { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual Employee? Employee { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
