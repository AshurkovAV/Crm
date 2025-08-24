using Crm.Domain.Common;

namespace Crm.Domain.Entities
{
    public class User : BaseEntity
    {
        // Приватный конструктор для контроля создания
        private User(
            string email
            )
        {
            Email = email;
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
            Role = UserRole.User; // Роль по умолчанию
        }

        // Свойства
        public string Email { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public bool IsActive { get; private set; }
        public UserRole Role { get; private set; }

        // Factory Method для создания пользователя
        public static User Create(
            string email)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty", nameof(email));

            if (!email.Contains('@'))
                throw new ArgumentException("Invalid email format", nameof(email));

            // Создание и возврат объекта
            return new User(
                email.ToLower() // Нормализация email
            );
        }

        // Доменные методы
        public void Deactivate()
        {
            IsActive = false;
        }       
    }
    public enum UserRole
    {
        User,
        Admin,
        Moderator
    }
}
