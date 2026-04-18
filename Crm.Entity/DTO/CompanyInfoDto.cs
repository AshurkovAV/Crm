
namespace Crm.Entity.DTO
{
    public class CompanyInfoDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string Role { get; set; }
        public DateTime? JoinedDate { get; set; }
        public bool IsCurrent { get; set; }
    }
}
