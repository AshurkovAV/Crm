using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

public partial class Employee
{
    public int EmployeeId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? Position { get; set; }

    public string? Department { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public DateOnly? HireDate { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<ClientInteraction> ClientInteractions { get; set; } = new List<ClientInteraction>();

    public virtual ICollection<CustomerOrder> CustomerOrders { get; set; } = new List<CustomerOrder>();

    public virtual ICollection<MaterialPurchase> MaterialPurchases { get; set; } = new List<MaterialPurchase>();

    public virtual ICollection<ProductionOrder> ProductionOrders { get; set; } = new List<ProductionOrder>();

    public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
