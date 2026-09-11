namespace Crm.Entity.Services;

/// <summary>
/// Анонимный доступ подрядчика к своему этапу по Magic Link (Модуль В ТЗ).
/// ПРИНЦИПИАЛЬНО без company-scoping через пользователя — единственная проверка доступа
/// это сам токен (Guid) и его срок действия. Не путать с "ProductionTaskRepository" цеха:
/// этот репозиторий только для изолированного анонимного сценария подрядчика.
/// </summary>
public interface IContractorPortalRepository
{
    Task<ContractorPortalResult> GetByTokenAsync(Guid token);

    Task<ContractorPortalActionResult> AcceptAsync(Guid token);

    Task<ContractorPortalActionResult> ReadyAsync(Guid token);
}

public enum ContractorPortalStatus
{
    Ok,
    NotFound,
    Expired
}

public sealed class ContractorPortalResult
{
    public ContractorPortalStatus Status { get; set; }
    public string? ContractorName { get; set; }
    public string? StageName { get; set; }
    public string? Notes { get; set; }
    public string? OrderItemName { get; set; }
    public string? DealTitle { get; set; }
    public DateTime? Deadline { get; set; }
    public string? TaskStatus { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool TokenUsed { get; set; }
}

public sealed class ContractorPortalActionResult
{
    public ContractorPortalStatus Status { get; set; }
    public string? Message { get; set; }
}
