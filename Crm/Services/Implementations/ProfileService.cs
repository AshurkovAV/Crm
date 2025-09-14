using Crm.Core.Interfaces;
using Crm.Core.ViewModels;
using Crm.Extensions;


namespace Crm.Core.Implementations
{
    public class ProfileService: IProfileService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProfileService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public ProfileViewModel GetProfileData()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            var user = session?.GetCurrentUser();

            if (user == null)
            {
                // Возвращаем данные по умолчанию или null
                return new ProfileViewModel
                {
                    FirstName = "Алексей",
                    LastName = "Ашурков",
                    Position = "Менеджер по продажам"
                };
            }

            return new ProfileViewModel
            {
                FirstName = user.FirstName ?? user.DefaultEmail,
                LastName = user.LastName ?? "",
                Position = user.Role ?? "Пользователь" // Или другое поле для должности
            };
        }
    }
}
