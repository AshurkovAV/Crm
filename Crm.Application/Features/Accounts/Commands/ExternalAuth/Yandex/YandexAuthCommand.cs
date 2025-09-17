using Crm.Application.Features.Accounts.Commands.CreateUser;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.Features.Accounts.Commands.ExternalAuth.Yandex
{
    public class YandexAuthCommand : IRequest<CreateUserResult>
    {
        public int Id { get; set; }

        public string? TokenType { get; set; }

        public string? AccessToken { get; set; }

        public int? ExpiresIn { get; set; }

        public string? RefreshToken { get; set; }

        public string? Scope { get; set; }

        public string? DeviceId { get; set; }

        public DateTime DateEdit { get; set; }
    }
}
