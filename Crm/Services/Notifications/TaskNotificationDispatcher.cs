namespace Crm.Services.Notifications
{
    /// <summary>
    /// Прогоняет уведомление о задаче через все зарегистрированные в DI каналы
    /// (IEnumerable&lt;ITaskNotificationChannel&gt; — ASP.NET Core сам соберёт все реализации).
    /// Ошибка одного канала не должна ронять остальные и не должна ронять сохранение задачи —
    /// поэтому ошибки только логируются.
    /// </summary>
    public class TaskNotificationDispatcher : ITaskNotificationDispatcher
    {
        private readonly IEnumerable<ITaskNotificationChannel> _channels;
        private readonly ILogger<TaskNotificationDispatcher> _logger;

        public TaskNotificationDispatcher(IEnumerable<ITaskNotificationChannel> channels, ILogger<TaskNotificationDispatcher> logger)
        {
            _channels = channels;
            _logger = logger;
        }

        public async Task DispatchAsync(TaskNotificationContext context)
        {
            foreach (var channel in _channels)
            {
                try
                {
                    await channel.NotifyAsync(context);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Канал уведомлений {Channel} не смог доставить уведомление по задаче {TaskId}",
                        channel.GetType().Name, context.TaskId);
                }
            }
        }
    }
}
