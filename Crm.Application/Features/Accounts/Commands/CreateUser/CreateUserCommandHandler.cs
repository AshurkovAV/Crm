using Crm.Entity.Entities;
using Crm.Entity.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Crm.Application.Features.Accounts.Commands.CreateUser
{
    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, CreateUserResult>
    {
        private IUserRepository _userRepository;
        private readonly ILogger<CreateUserCommandHandler> _logger;

        public CreateUserCommandHandler(IUserRepository userRepository,
            ILogger<CreateUserCommandHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<CreateUserResult> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Проверяем, существует ли пользователь
                var us = _userRepository.GetUser(request.Email);

                if (us.Success)
                {
                    return new CreateUserResult
                    {
                        Succeeded = false,
                        Errors = new List<string> { "Пользователь с таким адресом электронной почты уже существует" }
                    };
                }

                // 3. Создаем доменную сущность
                var user = User.Create(
                    request.Email                    
                    );

                // 4. Сохраняем в базу
                await _userRepository.AddAsync(user);               

                _logger.LogInformation("User created successfully with ID: {UserId}", user.Id);

                // 5. Возвращаем результат
                return new CreateUserResult
                {
                    Succeeded = true,
                    UserBase = user,
                    UserId = user.Id.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return new CreateUserResult
                {
                    Succeeded = false,
                    Errors = new List<string> { "An error occurred while creating user" }
                };
            }
        }
    }
}
