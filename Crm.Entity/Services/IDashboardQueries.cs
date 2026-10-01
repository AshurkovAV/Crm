namespace Crm.Entity.Services;

/// <summary>
/// Read-only запросы для главной страницы (дашборда). Ничего не изменяют в БД.
/// </summary>
public interface IDashboardQueries
{
    /// <param name="userId">Текущий пользователь.</param>
    /// <param name="includeSales">true — режим «Полная CRM»: дополнительно считаются сделки и производство.</param>
    Task<HomeDashboardData> GetHomeAsync(int userId, bool includeSales);
}

public sealed class HomeDashboardData
{
    public string? FirstName { get; set; }
    public string? DisplayName { get; set; }

    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }

    // --- Задачи (задачи не привязаны к компании: считаются по исполнителю = текущий пользователь)
    public int MyOpenTasks { get; set; }
    public int MyOverdueTasks { get; set; }
    public int MyDoneLast7Days { get; set; }
    public List<HomeTaskItem> MyTasks { get; set; } = new();

    // --- Проекты (по участию пользователя в проекте, как на странице «Проекты»)
    public int ActiveProjects { get; set; }
    public List<HomeProjectItem> Projects { get; set; } = new();

    // --- Команда (активные сотрудники текущей компании)
    public int TeamCount { get; set; }
    public List<HomeTeamMember> Team { get; set; } = new();

    // --- Только режим Full
    public bool SalesLoaded { get; set; }
    public int OpenDeals { get; set; }
    public decimal OpenDealsAmount { get; set; }
    public List<HomeDealStage> DealStages { get; set; } = new();

    /// <summary>Есть ли у компании хоть одно изделие с производственными этапами.</summary>
    public bool HasProduction { get; set; }
    public int OrdersInProduction { get; set; }
    public int OrdersInProductionOverdue { get; set; }
    public List<HomeProductionItem> Production { get; set; } = new();
}

public sealed class HomeTaskItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public DateTime? Deadline { get; set; }
    public string? Priority { get; set; }
    public int? Status { get; set; }
    public string? StatusName { get; set; }
}

public sealed class HomeProjectItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Status { get; set; }
    public int TotalTasks { get; set; }
    public int DoneTasks { get; set; }
    public int Members { get; set; }
}

public sealed class HomeTeamMember
{
    public int UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? Position { get; set; }
    public string? Role { get; set; }
    public string? DefaultAvatarId { get; set; }
    public string? IsAvatarEmpty { get; set; }
}

public sealed class HomeDealStage
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
    public bool IsClosed { get; set; }
}

public sealed class HomeProductionItem
{
    public int OrderItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DealTitle { get; set; }
    public string? ClientName { get; set; }
    public int TotalStages { get; set; }
    public int DoneStages { get; set; }
    public string? CurrentStage { get; set; }
    public DateTime? DueDate { get; set; }
}
