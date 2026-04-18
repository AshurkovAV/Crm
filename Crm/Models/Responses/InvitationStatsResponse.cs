namespace Crm.Models.Responses
{
    public class InvitationStatsResponse
    {
        /// <summary>
        /// Всего отправлено приглашений
        /// </summary>
        public int TotalSent { get; set; }

        /// <summary>
        /// Ожидают подтверждения
        /// </summary>
        public int Pending { get; set; }

        /// <summary>
        /// Принято
        /// </summary>
        public int Accepted { get; set; }

        /// <summary>
        /// Истек срок действия
        /// </summary>
        public int Expired { get; set; }

        /// <summary>
        /// Отклонено
        /// </summary>
        public int Declined { get; set; }

        /// <summary>
        /// Процент принятых приглашений
        /// </summary>
        public double AcceptanceRate { get; set; }

        /// <summary>
        /// Процент отказов
        /// </summary>
        public double DeclineRate => TotalSent > 0
            ? (double)Declined / TotalSent * 100
            : 0;

        /// <summary>
        /// Процент просроченных
        /// </summary>
        public double ExpiredRate => TotalSent > 0
            ? (double)Expired / TotalSent * 100
            : 0;

        /// <summary>
        /// Статистика по отделам
        /// </summary>
        public List<DepartmentInvitationStats> ByDepartment { get; set; } = new();

        /// <summary>
        /// Статистика по месяцам
        /// </summary>
        public List<MonthlyInvitationStats> ByMonth { get; set; } = new();
    }

    public class DepartmentInvitationStats
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public int Sent { get; set; }
        public int Accepted { get; set; }
        public double AcceptanceRate => Sent > 0 ? (double)Accepted / Sent * 100 : 0;
    }

    public class MonthlyInvitationStats
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; }
        public int Sent { get; set; }
        public int Accepted { get; set; }
    }
}
