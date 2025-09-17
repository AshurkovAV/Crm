using Crm.Application.Features.Accounts.Commands.CreateUser;
using Crm.Application.Features.Accounts.Services;
using Crm.Entity.Services;
using MediatR;

namespace Crm.Application.Features.Accounts.Commands.ExternalAuth.Yandex
{
    public class CreateUserYandexCommandHandler : IRequestHandler<CreateUserYandexCommand, CreateUserResult>
    {
        private IUserRepository _userRepository;
        private readonly IYandexAuthService _yandexAuthService;

        public CreateUserYandexCommandHandler(
            IUserRepository userRepository,
            IYandexAuthService yandexAuthService
            )
        {
            _yandexAuthService = yandexAuthService;
            _userRepository = userRepository;
        }
        public async Task<CreateUserResult> Handle(CreateUserYandexCommand request, CancellationToken cancellationToken)
        {
            // 1. Получаем токен от Яндекс
            var tokenResponse = await _yandexAuthService.GetTokenAsync(request.Code);

            // 2. Получаем информацию о пользователе
            var userInfo = await _yandexAuthService.GetUserInfoAsync(tokenResponse.AccessToken);

            // 3. Проверяем, существует ли пользователь
            var existingUser = _userRepository.GetUser(userInfo.DefaultEmail);
            var newUser = Crm.Entity.Entities.User.Create(
                userInfo.DefaultEmail);

            if (existingUser.Data != null)
            {
                newUser.Id = existingUser.Data.Id;              
            }

            //// 4. Создаем нового пользователя
            
            newUser.FirstName = userInfo.FirstName;
            newUser.LastName = userInfo.LastName;
            newUser.IdYandex = userInfo.Id;   
            newUser.Login = userInfo.Login;
            newUser.DisplayName = userInfo.DisplayName;
            newUser.RealName = userInfo.RealName;
            newUser.DefaultEmail = userInfo.DefaultEmail;
            newUser.IsActive = true;
            newUser.IsValidation = true;
            var result =  _userRepository.AddOrUpdateAsync(newUser);            

            return new CreateUserResult 
            { 
                UserBase = newUser,
                Succeeded = true,
                UserId = userInfo.Id,
            };
        }      
    }
}
