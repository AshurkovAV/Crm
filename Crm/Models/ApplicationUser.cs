using Crm.Entity.ModelsCrm;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crm.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        // Конструктор для установки значений по умолчанию
        public ApplicationUser()
        {
            CreatedDate = DateTime.UtcNow;
            ModifiedDate = DateTime.UtcNow;
            IsActive = true;
            IsValidation = false;
        }

        // === Дополнительные поля из вашей таблицы Users ===

        /// <summary>
        /// ID пользователя в Яндекс (если вход через Яндекс)
        /// </summary>
        public string? IdYandex { get; set; }

        /// <summary>
        /// Логин пользователя
        /// </summary>
        public string? Login { get; set; }

        /// <summary>
        /// Отображаемое имя (как будет видно другим пользователям)
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Реальное имя
        /// </summary>
        public string? RealName { get; set; }

        /// <summary>
        /// Имя
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// Фамилия
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// Пол
        /// </summary>
        public string? Sex { get; set; }

        /// <summary>
        /// Email по умолчанию (дублирует IdentityUser.Email, но может быть отдельный)
        /// </summary>
        public string? DefaultEmail { get; set; }

        /// <summary>
        /// День рождения
        /// </summary>
        public DateTime? Birthday { get; set; }

        /// <summary>
        /// ID аватара по умолчанию
        /// </summary>
        public string? DefaultAvatarId { get; set; }

        /// <summary>
        /// Пустой ли аватар
        /// </summary>
        public string? IsAvatarEmpty { get; set; }

        /// <summary>
        /// Роль пользователя (Admin, Manager, User и т.д.)
        /// </summary>
        public string? Role { get; set; }

        /// <summary>
        /// Активен ли пользователь
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Подтверждён ли пользователь
        /// </summary>
        public bool IsValidation { get; set; }

        /// <summary>
        /// Телефон по умолчанию
        /// </summary>
        public string? DefaultPhone { get; set; }

        /// <summary>
        /// ID устройства (для push-уведомлений)
        /// </summary>
        public string? DeviceId { get; set; }

        /// <summary>
        /// Текущая выбранная компания
        /// </summary>
        public int? CurrentCompanyId { get; set; }

        /// <summary>
        /// Дата создания
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Дата изменения
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        // === Навигационные свойства ===

        /// <summary>
        /// Текущая компания пользователя (навигационное свойство)
        /// </summary>
        [ForeignKey("CurrentCompanyId")]
        public virtual Company? CurrentCompany { get; set; }

        /// <summary>
        /// Компании, в которых состоит пользователь
        /// </summary>
        public virtual ICollection<CompanyUser>? CompanyUsers { get; set; }

        /// <summary>
        /// Проекты, в которых участвует пользователь
        /// </summary>
        public virtual ICollection<ProjectUser>? ProjectUsers { get; set; }

        /// <summary>
        /// Задачи, назначенные на пользователя
        /// </summary>
        public virtual ICollection<TaskCrm>? AssignedTasks { get; set; }

        /// <summary>
        /// Задачи, созданные пользователем
        /// </summary>
        public virtual ICollection<TaskCrm>? CreatedTasks { get; set; }
    }
}
