namespace Crm.Entity.ModelsCrm;

public partial class Deal
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public int OwnerId { get; set; }

    public string Title { get; set; } = null!;

    public string? ClientName { get; set; }

    public int? ClientId { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = "Новая";

    public DateTime? ExpectedCloseDate { get; set; }

    public string? Description { get; set; }

    public string? InstallationAddress { get; set; }

    public decimal? PrepaymentAmount { get; set; }

    public Guid PublicToken { get; set; } = Guid.NewGuid();

    public DateTime CreatedDate { get; set; }

    public DateTime ModifiedDate { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual Client? Client { get; set; }

    public virtual User Owner { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
