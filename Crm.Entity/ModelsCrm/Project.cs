using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

public partial class Project
{
    public int Id { get; set; }

    public int? Number { get; set; }

    public string Name { get; set; } = null!;

    public string? Activity { get; set; }

    public string? Efficiency { get; set; }

    public string? Role { get; set; }

    public string? AttachmentType { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual ICollection<ProjectUser> ProjectUsers { get; set; } = new List<ProjectUser>();
}
