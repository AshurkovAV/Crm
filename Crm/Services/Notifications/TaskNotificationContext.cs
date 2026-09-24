namespace Crm.Services.Notifications
{
    public enum TaskNotificationKind
    {
        Created,
        Updated
    }

    /// <summary>
    /// Данные для уведомления о задаче, не зависящие от канала доставки (email, позже — Макс и т.д.).
    /// Собирается один раз в TaskDataController и передаётся в TaskNotificationDispatcher,
    /// который прогоняет её через все зарегистрированные каналы.
    /// </summary>
    public class TaskNotificationContext
    {
        public TaskNotificationKind Kind { get; set; }
        public int TaskId { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? Deadline { get; set; }
        public string? Priority { get; set; }
        public string? ProjectName { get; set; }
        public string? AuthorName { get; set; }

        public int AssigneeUserId { get; set; }
        public string AssigneeName { get; set; } = string.Empty;
        public string? AssigneeEmail { get; set; }

        /// <summary>Что именно изменилось (для Kind == Updated) — по-человечески, для текста письма.</summary>
        public List<string> ChangedFields { get; set; } = new();
    }
}
