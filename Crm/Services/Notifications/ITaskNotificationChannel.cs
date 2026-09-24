namespace Crm.Services.Notifications
{
    /// <summary>
    /// Канал доставки уведомления о задаче. Сейчас в системе зарегистрирован только
    /// EmailTaskNotificationChannel. Чтобы добавить отправку в мессенджер Макс, достаточно
    /// реализовать этот интерфейс (например, MaxTaskNotificationChannel) и зарегистрировать
    /// его в Program.cs — TaskNotificationDispatcher подхватит его автоматически, без
    /// изменений в TaskDataController.
    /// </summary>
    public interface ITaskNotificationChannel
    {
        Task NotifyAsync(TaskNotificationContext context);
    }
}
