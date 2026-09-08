namespace Crm.Models.Deals
{
    public sealed class DealRow
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public int? ClientId { get; set; }
        public string? ClientName { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? ExpectedCloseDate { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
