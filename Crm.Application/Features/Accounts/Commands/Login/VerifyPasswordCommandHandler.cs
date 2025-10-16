using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Crm.Application.Features.Accounts.Commands.Login
{
    public class VerifyPasswordCommandHandler : IRequestHandler<VerifyPasswordCommand, VerifyPasswordResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<VerifyPasswordCommandHandler> _logger;

        public VerifyPasswordCommandHandler(IUserRepository userRepository,
            ILogger<VerifyPasswordCommandHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }
        public async Task<VerifyPasswordResult> Handle(VerifyPasswordCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Дополнительная проверка, что пользователь все еще существует
                var userResult = _userRepository.GetUser(request.Email);
                if (userResult.HasError)
                {
                    return VerifyPasswordResult.Failure("Invalid user data");
                }

                var passwordValid = await _userRepository.VerifyPasswordAsync(request.Email, request.Password);

                if (!passwordValid)
                {
                    _logger.LogWarning("Failed password verification for user {UserId}", request.UserId);
                    return VerifyPasswordResult.Failure("Invalid password");
                }

                // Создаем сессию, токен и т.д.
                var user = userResult.Data;
                return VerifyPasswordResult.Success(user, GenerateAuthToken(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password verification for user {UserId}", request.UserId);
                return VerifyPasswordResult.Failure("An error occurred during authentication");
            }
        }

        private string GenerateAuthToken(User user)
        {
            // Генерация JWT токена или сессии
            return "generated-token";
        }
    }
}
