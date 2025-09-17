using Crm.Application.Features.Accounts.Commands.CreateUser;
using MediatR;

namespace Crm.Application.Features.Accounts.Commands.ExternalAuth.Yandex
{
    public class CreateUserYandexCommand : IRequest<CreateUserResult>
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string? TokenType { get; set; }

        public string? AccessToken { get; set; }

        public int? ExpiresIn { get; set; }

        public string? RefreshToken { get; set; }

        public string? Scope { get; set; }

        public string? DeviceId { get; set; }

        public DateTime DateEdit { get; set; }
    }
}
