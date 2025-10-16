using Crm.Entity.ModelsCrm;
using MediatR;

namespace Crm.Application.Features.Accounts.Commands.Login
{
    public class LoginCommand : IRequest<LoginResult>
    {
        public string Email { get; set; } = string.Empty;
        public string? Password { get; set; }
    }

    public class LoginResult
    {
        public bool Succeeded { get; set; }
        public string? UserId { get; set; }
        public User? UserBase { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
