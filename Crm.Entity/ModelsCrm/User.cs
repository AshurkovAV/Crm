using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

public partial class User
{
    public int Id { get; set; }

    public string? IdYandex { get; set; }

    public string? Login { get; set; }

    public string? PasswordHash { get; set; }

    public string? PasswordSalt { get; set; }

    public string? ClientId { get; set; }

    public string? DisplayName { get; set; }

    public string? RealName { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Sex { get; set; }

    public string? DefaultEmail { get; set; }

    public DateTime? Birthday { get; set; }

    public string? DefaultAvatarId { get; set; }

    public string? IsAvatarEmpty { get; set; }

    public string? Role { get; set; }

    public bool IsActive { get; set; }

    public bool IsValidation { get; set; }

    public string? DefaultPhone { get; set; }

    public string? DeviceId { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime ModifiedDate { get; set; }

    public int? CurrentCompanyId { get; set; }

    /// <summary>
    /// Отчество пользователя
    /// </summary>
    public string? MiddleName { get; set; }

    /// <summary>
    /// Место рождения
    /// </summary>
    public string? BirthPlace { get; set; }

    /// <summary>
    /// Альтернативный email
    /// </summary>
    public string? AlternativeEmail { get; set; }

    public string? AlternativePhone { get; set; }

    public string? WorkPhone { get; set; }

    /// <summary>
    /// Telegram username
    /// </summary>
    public string? Telegram { get; set; }

    public string? WhatsApp { get; set; }

    /// <summary>
    /// Должность
    /// </summary>
    public string? Position { get; set; }

    public string? Department { get; set; }

    public string? Address { get; set; }

    public string? Bio { get; set; }

    public string? Website { get; set; }

    public string? LinkedIn { get; set; }

    public virtual ICollection<Company> Companies { get; set; } = new List<Company>();

    public virtual ICollection<CompanyUser> CompanyUsers { get; set; } = new List<CompanyUser>();

    public virtual ICollection<ProjectUser> ProjectUsers { get; set; } = new List<ProjectUser>();

    public virtual ICollection<RememberedDevice> RememberedDevices { get; set; } = new List<RememberedDevice>();
}
