using Crm.Core.Interfaces;
using Crm.Core.ViewModels;
using Crm.Entity.Services;
using Crm.Extensions;
using System.Security.Claims;


namespace Crm.Core.Implementations
{
    public class ProfileService: IProfileService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserRepository _userRepository;

        public ProfileService(
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _userRepository = userRepository;
        }

        public ProfileViewModel GetProfileData()
        {

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return GetDefaultProfile();
            }
                       
            var user = httpContext.User;

            // Проверяем, аутентифицирован ли пользователь
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                return GetDefaultProfile();
            }
            // Получаем ID пользователя из claims
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return GetProfileFromClaims(user); // Пытаемся получить из claims
            }
            // Если есть ID - получаем полные данные из БД
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                var dbUser = _userRepository.GetUser("");
                if (dbUser.Success)
                {
                    return new ProfileViewModel
                    {
                        FirstName = dbUser.Data.FirstName ?? GetEmailFromUser(user),
                        LastName = dbUser.Data.LastName ?? "",
                        Position = dbUser.Data.Role ?? "Пользователь",                    
                    };
                }
            }

            // Если не удалось получить из БД - возвращаем данные из claims
            return GetProfileFromClaims(user);


        }


        // Метод для получения данных из claims (если нет доступа к БД)
        private ProfileViewModel GetProfileFromClaims(ClaimsPrincipal user)
        {
            var email = GetEmailFromUser(user);
            var fullNameClaim = user.FindFirst("FullName")?.Value;

            return new ProfileViewModel
            {
                FirstName = user.FindFirstValue(ClaimTypes.GivenName)
                    ?? (fullNameClaim?.Split(' ').FirstOrDefault())
                    ?? email?.Split('@').FirstOrDefault()
                    ?? "Пользователь",
                LastName = user.FindFirstValue(ClaimTypes.Surname)
                    ?? (fullNameClaim?.Split(' ').Skip(1).FirstOrDefault())
                    ?? "",               
                Position = user.FindFirstValue(ClaimTypes.Role)
                    ?? user.FindFirst("Position")?.Value
                    ?? "Пользователь"
            };
        }

        // Вспомогательный метод для получения email
        private string? GetEmailFromUser(ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.Email)
                ?? user.FindFirstValue("email")
                ?? user.Identity?.Name;
        }


        private ProfileViewModel GetDefaultProfile()
        {
            return new ProfileViewModel
            {
                FirstName = "Гость",
                LastName = "",
                Position = "Неавторизованный пользователь",                 
            };
        }
    }
}
