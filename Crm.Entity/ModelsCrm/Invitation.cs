using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

public partial class Invitation
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public DateTime? DeclinedAt { get; set; }

    public int? CreatedBy { get; set; }

    public string? Message { get; set; }

    public string InvitationType { get; set; } = null!;

    public int? CompanyId { get; set; }

    public int? RoleId { get; set; }
}
