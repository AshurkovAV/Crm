using Crm.Services.Email;

namespace Crm.Services.Notifications
{
    /// <summary>Канал уведомлений о задаче по email — первая (и пока единственная) реализация.</summary>
    public class EmailTaskNotificationChannel : ITaskNotificationChannel
    {
        private readonly IEmailService _emailService;

        public EmailTaskNotificationChannel(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task NotifyAsync(TaskNotificationContext context)
        {
            if (string.IsNullOrWhiteSpace(context.AssigneeEmail))
                return;

            await _emailService.SendTaskNotificationEmailAsync(context);
        }
    }
}
