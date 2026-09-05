namespace Crm.Entity.ModelsCrm;

public partial class ChatMessage
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public int SenderUserId { get; set; }

    public int RecipientUserId { get; set; }

    public string Text { get; set; } = null!;

    public DateTime SentAt { get; set; }

    public bool IsRead { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual User SenderUser { get; set; } = null!;

    public virtual User RecipientUser { get; set; } = null!;
}
