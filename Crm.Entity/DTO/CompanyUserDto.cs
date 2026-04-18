namespace Crm.Entity.DTO
{
    public class CompanyUserDto
    {
        public int UserId { get; set; }
        public string Login { get; set; }
        public string DisplayName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public string Position { get; set; }
        public DateTime? JoinedDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsOwner { get; set; }
        public bool IsCurrentUser { get; set; }
        public string AvatarUrl { get; set; }

        // Информация о компании
        public int? CompanyId { get; set; }
        public string CompanyName { get; set; }

        // Вычисляемые свойства
        public string DisplayRole => string.IsNullOrEmpty(Role) ? "Сотрудник" : Role;
        public string FullName => string.IsNullOrEmpty(DisplayName)
            ? $"{FirstName} {LastName}".Trim()
            : DisplayName;
        public string StatusBadge => IsOwner ? "Владелец" : (IsCurrentUser ? "Вы" : DisplayRole);
    }
}
