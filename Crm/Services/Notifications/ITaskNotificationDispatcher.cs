namespace Crm.Services.Notifications
{
    public interface ITaskNotificationDispatcher
    {
        /// <summary>Рассылает уведомление о задаче через все зарегистрированные каналы.</summary>
        Task DispatchAsync(TaskNotificationContext context);
    }
}
