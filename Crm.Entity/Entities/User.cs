namespace Crm.Entity.Entities
{
    public class User : ModelsCrm.User
    {
        // Приватный конструктор для контроля создания
        private User(
            string email
            )
        {
            DefaultEmail = email;
            CreatedDate = DateTime.UtcNow;
            ModifiedDate = DateTime.UtcNow;
            IsActive = true;
            Role = UserRole.User.ToString(); // Роль по умолчанию
        }               

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
