using Crm.Services.Notifications;

namespace Crm.Services.Email
{
    public interface IEmailService
    {
        /// <returns>true — письмо ушло, false — SMTP вернул ошибку (она уже записана в лог)</returns>
        Task<bool> SendInvitationEmailAsync(string email, string link, string customMessage = null, string companyName = "Crm System", int expiryDays = 7, string inviterName = null);

        /// <summary>Письмо исполнителю о новой/изменённой задаче.</summary>
        Task SendTaskNotificationEmailAsync(TaskNotificationContext context);
    }
}
