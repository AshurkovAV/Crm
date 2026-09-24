using Crm.Services.Notifications;

namespace Crm.Services.Email
{
    public interface IEmailService
    {
        Task SendInvitationEmailAsync(string email, string link, string customMessage = null, string companyName = "Crm System");

        /// <summary>Письмо исполнителю о новой/изменённой задаче.</summary>
        Task SendTaskNotificationEmailAsync(TaskNotificationContext context);
    }
}
