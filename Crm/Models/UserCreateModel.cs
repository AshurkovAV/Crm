using System.ComponentModel.DataAnnotations;

namespace Crm.Models
{
    public class UserCreateModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string PhoneNumber { get; set; }

        // Для внешней авторизации (VK, Yandex и т.д.)
        public string ExternalId { get; set; }

        public string Provider { get; set; } // "VK", "Yandex", "Google"

        public string Password { get; set; } // Для обычной регистрации

        // Дополнительные поля
        public string AvatarUrl { get; set; }

        public DateTime? BirthDate { get; set; }

        public string TimeZone { get; set; }
    }
}
