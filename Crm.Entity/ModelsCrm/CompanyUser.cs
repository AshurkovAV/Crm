using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

public partial class CompanyUser
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public int UserId { get; set; }

    public string Role { get; set; } = null!;

    public string? Position { get; set; }

    public DateTime? JoinedDate { get; set; }

    public bool IsActive { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
