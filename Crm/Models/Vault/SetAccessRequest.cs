namespace Crm.Models.Vault
{
    public sealed class SetAccessRequest
    {
        public int EntryId { get; set; }
        public List<int> UserIds { get; set; } = new();
    }
}
