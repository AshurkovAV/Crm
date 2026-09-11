namespace Crm.Models.Production;

/// <summary>
/// Карточка производственного этапа для канбан-доски цеха.
/// </summary>
public sealed class ProductionTaskCardDto
{
    public int ProductionTaskId { get; set; }
    public int OrderItemId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public int StageOrder { get; set; }
    public bool IsFinalStage { get; set; }
    public string ExecutorType { get; set; } = "Internal";
    public int? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public int? ContractorId { get; set; }
    public string? ContractorName { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Notes { get; set; }

    public string OrderItemName { get; set; } = string.Empty;
    public int DealId { get; set; }
    public string DealTitle { get; set; } = string.Empty;
    public string? ClientName { get; set; }
}
