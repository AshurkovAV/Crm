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

    /// <summary>
    /// Строка подключения из конфигурации (ConnectionStrings:Crm) — выставляется в Program.cs при старте.
    /// В исходниках пароля к базе нет: задайте её в appsettings.Production.json / appsettings.Development.json
    /// на своей машине (не коммитить) или переменной окружения ConnectionStrings__Crm.
    /// </summary>
    public static string? ConfiguredConnectionString { get; set; }

    private static string ResolveConnectionString() =>
        ConnectionStringOverride
        ?? ConfiguredConnectionString
        ?? throw new InvalidOperationException(
            "Не задана строка подключения ConnectionStrings:Crm (appsettings.Production.json или переменная окружения ConnectionStrings__Crm).");
}
