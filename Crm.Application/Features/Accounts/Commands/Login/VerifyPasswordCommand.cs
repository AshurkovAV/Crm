using Crm.Entity.ModelsCrm;
using MediatR;

namespace Crm.Application.Features.Accounts.Commands.Login
{
    public class VerifyPasswordCommand : IRequest<VerifyPasswordResult>
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class VerifyPasswordResult
    {
        public bool Succeeded { get; set; }
        public User User { get; set; }
        public string Token { get; set; }
        public List<string> Errors { get; set; }

        public static VerifyPasswordResult Success(User user, string token)
            => new() { Succeeded = true, User = user, Token = token };

        public static VerifyPasswordResult Failure(string error)
            => new() { Succeeded = false, Errors = new List<string> { error } };
    }
}
