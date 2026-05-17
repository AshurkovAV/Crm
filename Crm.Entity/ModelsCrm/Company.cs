using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

public partial class Company
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int OwnerId { get; set; }

    public string? InviteCode { get; set; }

    public int? MaxProjects { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime ModifiedDate { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<CompanyUser> CompanyUsers { get; set; } = new List<CompanyUser>();

    public virtual User Owner { get; set; } = null!;
}
