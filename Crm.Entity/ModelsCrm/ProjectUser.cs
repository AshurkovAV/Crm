using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

public partial class ProjectUser
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public int UserId { get; set; }

    public string Role { get; set; } = null!;

    public DateTime? JoinedDate { get; set; }

    public bool IsActive { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
