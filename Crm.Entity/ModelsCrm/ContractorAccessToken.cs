using System;

namespace Crm.Entity.ModelsCrm;

/// <summary>
/// Magic Link подрядчика: анонимный токен-доступ к одному производственному этапу.
/// </summary>
public partial class ContractorAccessToken
{
    public int ContractorAccessTokenId { get; set; }

    public int ContractorId { get; set; }

    public int ProductionTaskId { get; set; }

    public Guid Token { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public virtual Contractor Contractor { get; set; } = null!;

    public virtual ProductionTask ProductionTask { get; set; } = null!;
}
