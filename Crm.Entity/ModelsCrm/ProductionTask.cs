using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

/// <summary>
/// Производственный этап изделия (Резка/Шлифовка/Сборка/ОТК), Модуль Б и В ТЗ.
/// ExecutorType = "External" означает, что этап выполняет подрядчик (ContractorId),
/// иначе — внутренний сотрудник (AssignedUserId).
/// </summary>
public partial class ProductionTask
{
    public int ProductionTaskId { get; set; }

    public int OrderItemId { get; set; }

    public string StageName { get; set; } = null!;

    public int StageOrder { get; set; }

    public string ExecutorType { get; set; } = "Internal";

    public int? AssignedUserId { get; set; }

    public int? ContractorId { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? PhotoUrl { get; set; }

    public string? Notes { get; set; }

    public virtual OrderItem OrderItem { get; set; } = null!;

    public virtual User? AssignedUser { get; set; }

    public virtual Contractor? Contractor { get; set; }

    public virtual ICollection<ContractorAccessToken> ContractorAccessTokens { get; set; } = new List<ContractorAccessToken>();
}
