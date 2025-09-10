using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

public partial class User
{
    public int Id { get; set; }

    public string? IdYandex { get; set; }

    public string? Login { get; set; }

    public string? ClientId { get; set; }

    public string? DisplayName { get; set; }

    public string? RealName { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Sex { get; set; }

    public string? DefaultEmail { get; set; }

    public DateOnly? Birthday { get; set; }

    public string? DefaultAvatarId { get; set; }

    public string? IsAvatarEmpty { get; set; }

    public string? Role { get; set; }

    public bool IsActive { get; set; }

    public bool IsValidation { get; set; }

    public string? DefaultPhone { get; set; }

    public string? DeviceId { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime ModifiedDate { get; set; }
}
