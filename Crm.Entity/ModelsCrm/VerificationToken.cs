using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

public partial class VerificationToken
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string Token { get; set; } = null!;

    public DateTime Expiration { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsUsed { get; set; }
}
