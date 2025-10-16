using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

public partial class RememberedDevice
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string RememberToken { get; set; } = null!;

    public string DeviceId { get; set; } = null!;

    public int UserId { get; set; }

    public DateTime Expiration { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
