using Crm.Entity.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Crm.Application.Features.Accounts.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
    {
        private IUserRepository _userRepository;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(IUserRepository userRepository,
            ILogger<LoginCommandHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Проверяем, существует ли пользователь
                var us = _userRepository.GetUser(request.Email);

                if (us.HasError)
                {
                    return new LoginResult
                    {
                        Succeeded = false,
                        Errors = new List<string> { us.LastError.Message }

                    };
                }

                if (!us.Data.IsValidation)
                {
                    return new LoginResult
                    {
                        Succeeded = false,
                        UserId = us.Data.Id.ToString(),
                        Errors = new List<string> { "Пользователь не подтвердил email" }

                    };
                }

                //var verify = _userRepository.VerifyPasswordAsync(request.Email, request.Password);

                //if (!verify.Result)
                //{
                //    return new LoginResult
                //    {
                //        Succeeded = false,
                //        UserId = verify.Id.ToString(),
                //        Errors = new List<string> { "Пароль не верный" }
                //    };
                //}

                // 5. Возвращаем результат
                return new LoginResult
                {
                    Succeeded = true,
                    UserBase = us.Data,
                    UserId = us.Data.Id.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return new LoginResult
                {
                    Succeeded = false,
                    Errors = new List<string> { "An error occurred while creating user" }
                };
            }
        }
    }
}
