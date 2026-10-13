namespace Crm.Entity.ModelsCrm;

public partial class CrmContext
{
    /// <summary>
    /// Необязательная подмена строки подключения через переменную окружения CRM_DB_CONNECTION
    /// (локальный запуск против другой базы, например SQL Server в docker). Если переменная
    /// не задана — используется строка подключения из OnConfiguring, как и раньше.
    /// </summary>
    private static readonly string? ConnectionStringOverride =
        Environment.GetEnvironmentVariable("CRM_DB_CONNECTION") is { Length: > 0 } value ? value : null;
}
