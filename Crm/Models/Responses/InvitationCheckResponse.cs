namespace Crm.Models.Responses
{
    public class InvitationCheckResponse
    {
        /// <summary>
        /// Действительно ли приглашение
        /// </summary>
        public bool Valid { get; set; }

        /// <summary>
        /// Сообщение об ошибке, если приглашение недействительно
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Email, на который было отправлено приглашение
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Телефон, на который было отправлено приглашение
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// ID отдела, в который приглашается сотрудник
        /// </summary>
        public int? DepartmentId { get; set; }

        /// <summary>
        /// ID компании
        /// </summary>
        public int? CompanyId { get; set; }

        /// <summary>
        /// Название компании
        /// </summary>
        public string CompanyName { get; set; }

        /// <summary>
        /// Название отдела
        /// </summary>
        public string DepartmentName { get; set; }

        /// <summary>
        /// Должность, на которую приглашают
        /// </summary>
        public string Position { get; set; }

        /// <summary>
        /// Роль, которая будет назначена
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Тип приглашения (Email, SMS, Link)
        /// </summary>
        public string InvitationType { get; set; }

        /// <summary>
        /// Дата создания приглашения
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Дата истечения срока действия
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Дата принятия приглашения
        /// </summary>
        public DateTime? AcceptedAt { get; set; }

        /// <summary>
        /// Статус приглашения (Pending, Accepted, Expired, Cancelled)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Оставшееся время действия в днях
        /// </summary>
        public int? DaysLeft => ExpiresAt.HasValue
            ? (int)Math.Ceiling((ExpiresAt.Value - DateTime.UtcNow).TotalDays)
            : null;

        /// <summary>
        /// Просрочено ли приглашение
        /// </summary>
        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

        /// <summary>
        /// Может ли приглашение быть принято
        /// </summary>
        public bool CanBeAccepted => Valid && !IsExpired && Status == "Pending";
    }
}