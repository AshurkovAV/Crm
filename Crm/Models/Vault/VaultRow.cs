namespace Crm.Models.Vault
{
    public sealed class VaultRow
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Login { get; set; }
        public string? Password { get; set; }
        public string? Url { get; set; }
        public string? Notes { get; set; }
        public string? Tags { get; set; }
        public int CreatedByUserId { get; set; }
        public bool IsOwner { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
