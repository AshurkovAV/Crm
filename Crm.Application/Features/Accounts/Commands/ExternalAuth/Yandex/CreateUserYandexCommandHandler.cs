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

            // У части аккаунтов Яндекс отдаёт default_email пустым, хотя список emails
            // заполнен. Без подстраховки User.Create('') бросал исключение и вход падал.
            var email = !string.IsNullOrWhiteSpace(userInfo.DefaultEmail)
                ? userInfo.DefaultEmail
                : userInfo.Emails?.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e));

            if (string.IsNullOrWhiteSpace(email))
            {
                return new CreateUserResult
                {
                    Succeeded = false,
                    Errors = { "Яндекс не передал email — проверьте права доступа (scope) приложения." }
                };
            }

            // 3. Проверяем, существует ли пользователь.
            // Существующую запись обновляем на месте: AddOrUpdateAsync делает db.Users.Update(),
            // который перезаписывает ВСЕ колонки, поэтому новый объект с одним лишь Id
            // обнулял бы пароль, текущую компанию и остальной профиль.
            var existingUser = _userRepository.GetUser(email);
            var user = existingUser.Data ?? Crm.Entity.Entities.User.Create(email);

            // 4. Данные от Яндекса обновляем при каждом входе
            user.FirstName = userInfo.FirstName;
            user.LastName = userInfo.LastName;
            user.IdYandex = userInfo.Id;
            user.Login = userInfo.Login;
            user.DisplayName = userInfo.DisplayName;
            user.RealName = userInfo.RealName;
            user.IsActive = true;
            user.IsValidation = true;
            user.ModifiedDate = DateTime.UtcNow;

            // Обязательно ждём сохранения: иначе вход выполнялся раньше, чем новый
            // пользователь попадал в БД, а ошибки сохранения терялись молча.
            await _userRepository.AddOrUpdateAsync(user);

            return new CreateUserResult
            {
                UserBase = user as Crm.Entity.Entities.User,
                User = user,
                Succeeded = true,
                UserId = userInfo.Id,
            };
        }      
    }
}
