namespace Crm.Entity.Services;

/// <summary>
/// Назначение производственного этапа внешнему подрядчику и генерация Magic Link (Модуль В ТЗ).
/// Работает поверх ProductionTask/ContractorAccessToken напрямую через CrmContext
/// (не конкурирует с репозиторием цеха — только сценарий "передать этап подрядчику").
/// </summary>
public interface IContractorAssignmentRepository
{
    /// <summary>
    /// Список этапов производства текущей компании пользователя, ещё не завершённых,
    /// которые можно передать подрядчику (для выпадающего списка на форме назначения).
    /// </summary>
    Task<List<AssignableStageRow>> GetAssignableStagesAsync(int userId);

    /// <summary>
    /// Помечает этап как выполняемый подрядчиком, создаёт токен Magic Link.
    /// Возвращает относительный URL вида /contractor/{token} или null, если этап/подрядчик не найдены
    /// либо не принадлежат текущей компании пользователя.
    /// </summary>
    Task<ContractorAssignResult?> AssignAndCreateLinkAsync(int productionTaskId, int contractorId, int userId, int? expiresInDays);
}

public sealed class AssignableStageRow
{
    public int ProductionTaskId { get; set; }
    public string StageName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string ExecutorType { get; set; } = null!;
    public string? ContractorName { get; set; }
    public string OrderItemName { get; set; } = null!;
    public string DealTitle { get; set; } = null!;
}

public sealed class ContractorAssignResult
{
    public Guid Token { get; set; }
    public string RelativeUrl { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
