using Crm.Entity.ModelsCrm;

namespace Crm.Entity.Services;

/// <summary>
/// Результат попытки сменить статус производственного этапа.
/// </summary>
public sealed class ProductionTaskStatusResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public ProductionTask? Task { get; init; }

    public static ProductionTaskStatusResult Ok(ProductionTask task) => new() { Success = true, Task = task };
    public static ProductionTaskStatusResult Fail(string error) => new() { Success = false, Error = error };
}

public interface IProductionTaskRepository
{
    /// <summary>
    /// Список этапов конкретного изделия (company-scoped через Deal).
    /// </summary>
    Task<List<ProductionTask>> GetByOrderItemAsync(int orderItemId, int userId);

    /// <summary>
    /// Плоский список активных этапов всей компании для канбан-доски цеха.
    /// </summary>
    Task<List<ProductionTask>> GetByCompanyKanbanAsync(int userId, string? stageName = null, string? status = null);

    /// <summary>
    /// Создаёт этапы по дефолтному списку (Резка/Обработка/Сборка/ОТК), если для изделия их ещё нет.
    /// Возвращает актуальный список этапов изделия (существующие + вновь созданные).
    /// </summary>
    Task<List<ProductionTask>?> GenerateDefaultStagesAsync(int orderItemId, int userId);

    /// <summary>
    /// Смена статуса этапа с проверкой обязательного фото на ОТК/последнем этапе.
    /// </summary>
    Task<ProductionTaskStatusResult> UpdateStatusAsync(int taskId, int userId, string newStatus, string? photoUrl);

    /// <summary>
    /// Назначить внутреннего исполнителя на этап.
    /// </summary>
    Task<bool> AssignUserAsync(int taskId, int userId, int? assignedUserId);

    /// <summary>
    /// Добавить произвольный этап к изделию вручную.
    /// </summary>
    Task<ProductionTask?> AddStageAsync(int orderItemId, int userId, string stageName, string executorType, int? assignedUserId, int? contractorId);

    /// <summary>
    /// Удалить этап (нельзя удалить уже завершённый — для сохранения истории).
    /// </summary>
    Task<bool> DeleteStageAsync(int taskId, int userId);
}
