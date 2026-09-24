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
            // заполнен (известная особенность их API) — без этой подстраховки email
            // пользователя терялся, GetUser('') не находил его, и весь вход падал.
            var email = !string.IsNullOrWhiteSpace(userInfo.DefaultEmail)
                ? userInfo.DefaultEmail
                : userInfo.Emails?.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e));

            if (string.IsNullOrWhiteSpace(email))
            {
                return new CreateUserResult
                {
                    Succeeded = false,
                    Errors = { "Яндекс не передал ни одного email — проверьте права доступа (scope) приложения." }
                };
            }

            // 3. Проверяем, существует ли пользователь.
            var existingUser = _userRepository.GetUser(email);
            var newUser = Crm.Entity.Entities.User.Create(email);

            // ВАЖНО: AddOrUpdateAsync делает db.Users.Update(user), который перезаписывает
            // ВСЕ колонки строки, а не только изменённые. User.Create() задаёт лишь горстку
            // полей — если для существующего пользователя скопировать в newUser только Id
            // (как было раньше), при сохранении обнулятся PasswordHash/PasswordSalt,
            // CurrentCompanyId и вообще весь остальной профиль. Поэтому сначала переносим
            // ВСЕ поля существующей записи, и только потом поверх накатываем данные от Яндекса.
            if (existingUser.Data != null)
            {
                var existing = existingUser.Data;
                newUser.Id = existing.Id;
                newUser.PasswordHash = existing.PasswordHash;
                newUser.PasswordSalt = existing.PasswordSalt;
                newUser.ClientId = existing.ClientId;
                newUser.Sex = existing.Sex;
                newUser.Birthday = existing.Birthday;
                newUser.DefaultAvatarId = existing.DefaultAvatarId;
                newUser.IsAvatarEmpty = existing.IsAvatarEmpty;
                newUser.Role = existing.Role;
                newUser.DefaultPhone = existing.DefaultPhone;
                newUser.DeviceId = existing.DeviceId;
                newUser.CreatedDate = existing.CreatedDate;
                newUser.CurrentCompanyId = existing.CurrentCompanyId;
                newUser.MiddleName = existing.MiddleName;
                newUser.BirthPlace = existing.BirthPlace;
                newUser.AlternativeEmail = existing.AlternativeEmail;
                newUser.AlternativePhone = existing.AlternativePhone;
                newUser.WorkPhone = existing.WorkPhone;
                newUser.Telegram = existing.Telegram;
                newUser.WhatsApp = existing.WhatsApp;
                newUser.Position = existing.Position;
                newUser.Department = existing.Department;
                newUser.Address = existing.Address;
                newUser.Bio = existing.Bio;
                newUser.Website = existing.Website;
                newUser.LinkedIn = existing.LinkedIn;
            }

            // Данные от Яндекса — их и правда обновляем при каждом входе.
            newUser.FirstName = userInfo.FirstName;
            newUser.LastName = userInfo.LastName;
            newUser.IdYandex = userInfo.Id;
            newUser.Login = userInfo.Login;
            newUser.DisplayName = userInfo.DisplayName;
            newUser.RealName = userInfo.RealName;
            newUser.DefaultEmail = email;
            newUser.IsActive = true;
            newUser.IsValidation = true;
            newUser.ModifiedDate = DateTime.UtcNow;
            // Раньше здесь не было await — метод возвращал Task, который никто не ждал
            // (fire-and-forget). Обработчик мог вернуть результат и уйти в редирект раньше,
            // чем сохранение реально попадёт в БД, а любая ошибка сохранения потерялась бы молча.
            await _userRepository.AddOrUpdateAsync(newUser);

            return new CreateUserResult
            {
                UserBase = newUser,
                Succeeded = true,
                UserId = userInfo.Id,
            };
        }      
    }
}
