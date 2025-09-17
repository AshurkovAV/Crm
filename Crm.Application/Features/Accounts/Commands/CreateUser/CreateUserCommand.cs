using Crm.Entity.Entities;
using Crm.Entity.ModelsCrm;
using MediatR;

namespace Crm.Application.Features.Accounts.Commands.CreateUser
{
    public class CreateUserCommand : IRequest<CreateUserResult>
    {
        public string Email { get; set; } = string.Empty;                
    }

    // Result DTO
    public class CreateUserResult
    {
        public bool Succeeded { get; set; }
        public string? UserId { get; set; }
        public Crm.Entity.Entities.User UserBase { get; set; }
        public UserToken UserToken { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
