namespace Crm.Models.Production;

public sealed class GenerateStagesRequest
{
    public int OrderItemId { get; set; }
}

public sealed class ProductionTaskStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
}

public sealed class AssignUserRequest
{
    public int? AssignedUserId { get; set; }
}

public sealed class AddStageRequest
{
    public int OrderItemId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string ExecutorType { get; set; } = "Internal";
    public int? AssignedUserId { get; set; }
    public int? ContractorId { get; set; }
}
