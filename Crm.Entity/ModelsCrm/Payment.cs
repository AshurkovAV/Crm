using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int? OrderId { get; set; }

    public DateTime? PaymentDate { get; set; }

    public decimal Amount { get; set; }

    public string? PaymentMethod { get; set; }

    public string? Notes { get; set; }

    public virtual CustomerOrder? Order { get; set; }
}
