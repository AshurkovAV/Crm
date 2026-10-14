using System;

namespace Crm.Entity.ModelsCrm;

/// <summary>
/// Привязка пользователя CRM к мессенджеру MAX. Одноразовый токен для ссылки https://max.ru/&lt;bot&gt;?start=&lt;token&gt;
/// хранится только как SHA-256 (LinkTokenHash) и живёт 15 минут. MaxUserId — куда слать сообщения.
/// </summary>
public partial class UserMaxLink
{
    public int UserId { get; set; }

    public long? MaxUserId { get; set; }

    public DateTime? LinkedAt { get; set; }

    public string? LinkTokenHash { get; set; }

    public DateTime? LinkTokenExpiresAt { get; set; }
}
