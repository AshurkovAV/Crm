using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

public partial class Client
{
    public int ClientId { get; set; }

    public string Name { get; set; } = null!;

    public string? ContactPerson { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? TaxNumber { get; set; }

    public string? Notes { get; set; }

    public DateTime? CreatedDate { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<ClientInteraction> ClientInteractions { get; set; } = new List<ClientInteraction>();

    public virtual ICollection<CustomerOrder> CustomerOrders { get; set; } = new List<CustomerOrder>();
}
