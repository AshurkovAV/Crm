using MediatR;

namespace Crm.Application.Features.Accounts.Commands.CreateCompany
{
    public class CreateCompanyCommand : IRequest<CreateCompanyResult>
    {
        public string CompanyName { get; set; } = string.Empty;
        public int    MaxProjects { get; set; } = 10;
        public int    OwnerId { get; set; }
        
    }

    // Result DTO
    public class CreateCompanyResult
    {
        public bool Succeeded { get; set; }       
        public string? CompanyId { get; set; }
        public Crm.Entity.Entities.Company CompanyBase { get; set; }        
        public List<string> Errors { get; set; } = new();
    }
}
