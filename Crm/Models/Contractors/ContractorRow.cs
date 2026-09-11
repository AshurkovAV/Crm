namespace Crm.Models.Contractors
{
    public sealed class ContractorRow
    {
        public int ContractorId { get; set; }
        public string Name { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
    }
}
