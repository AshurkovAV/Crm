namespace Crm.Models.Contractors
{
    public sealed class AssignContractorRequest
    {
        public int ProductionTaskId { get; set; }
        public int ContractorId { get; set; }
        public int? ExpiresInDays { get; set; }
    }
}
