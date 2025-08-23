using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

public partial class ClientInteraction
{
    public int InteractionId { get; set; }

    public int ClientId { get; set; }

    public DateTime? InteractionDate { get; set; }

    public string? InteractionType { get; set; }

    public string? Description { get; set; }

    public int? EmployeeId { get; set; }

    public DateTime? FollowUpDate { get; set; }

    public bool? FollowUpDone { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual Employee? Employee { get; set; }
}
