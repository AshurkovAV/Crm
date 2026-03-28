using Crm.Core.Features.Account.Interfaces;
using Crm.Entity.ModelsCrm;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Crm.Infrastructure.Services
{
    public class AuthenticationService : Application.Interfaces.IAuthenticationService
    {
        private readonly IRememberDeviceService _rememberDeviceService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthenticationService(
            IRememberDeviceService rememberDeviceService, 
            IHttpContextAccessor   httpContextAccessor)
        {
            _rememberDeviceService = rememberDeviceService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task CreateRememberTokenAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                return;

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return;

            // Удаляем старый токен (если есть)
            await ClearRememberTokenAsync(email);

            // Генерируем новый токен и device ID
            var deviceId = Guid.NewGuid().ToString();
            var token = GenerateSecureToken();

            // Сохраняем в БД (хэшированный)
            await _rememberDeviceService.SaveRememberTokenAsync(email, token, deviceId);

            // Устанавливаем cookies
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = httpContext.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.Now.AddDays(30)
            };

            httpContext.Response.Cookies.Append("remember_email", email, cookieOptions);
            httpContext.Response.Cookies.Append("remember_token", token, cookieOptions);
            httpContext.Response.Cookies.Append("device_id", deviceId, cookieOptions);
        }

        public async Task ClearRememberTokenAsync(string email)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return;

            var deviceId = httpContext.Request.Cookies["device_id"];

            // Удаляем из БД
            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(deviceId))
            {
                await _rememberDeviceService.RemoveRememberTokenAsync(email, deviceId);
            }

            // Удаляем cookies
            httpContext.Response.Cookies.Delete("remember_email");
            httpContext.Response.Cookies.Delete("remember_token");
            httpContext.Response.Cookies.Delete("device_id");
        }

        public async Task AuthenticateWithCookiesAsync(User user)
        {
            // Создаем claims с данными пользователя
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.DefaultEmail ?? string.Empty),
                new Claim(ClaimTypes.Name, user.DisplayName ?? user.DefaultEmail ?? "User"),
            };

            // Дополнительные claims из UserBase
            if (!string.IsNullOrEmpty(user.FirstName))
                claims.Add(new Claim(ClaimTypes.GivenName, user.FirstName));

            if (!string.IsNullOrEmpty(user.LastName))
                claims.Add(new Claim(ClaimTypes.Surname, user.LastName));

            if (!string.IsNullOrEmpty(user.Role))
                claims.Add(new Claim("Position", user.Role));

            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2),
                AllowRefresh = true,
                IssuedUtc = DateTimeOffset.UtcNow
            };

            // Выполняем аутентификацию (создаем куки)
            await _httpContextAccessor.HttpContext?.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        public async Task UpdateRememberCookiesAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                return;

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return;

            var rememberEmail = httpContext.Request.Cookies["remember_email"];
            var rememberToken = httpContext.Request.Cookies["remember_token"];
            var deviceId = httpContext.Request.Cookies["device_id"];

            // Проверяем, что cookies существуют и валидны
            if (rememberEmail != email ||
                string.IsNullOrEmpty(rememberToken) ||
                string.IsNullOrEmpty(deviceId))
            {
                return;
            }

            // Проверяем валидность токена в БД
            var isValid = await _rememberDeviceService.ValidateRememberTokenAsync(
                email, rememberToken, deviceId);

            if (!isValid) return;

            // Обновляем срок действия в БД (если у вас есть такой метод)
           // await _rememberDeviceService.ExtendTokenExpirationAsync(email, deviceId);

            // Обновляем срок действия cookies
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = httpContext.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.Now.AddDays(30)
            };

            httpContext.Response.Cookies.Append("remember_email", email, cookieOptions);
            httpContext.Response.Cookies.Append("remember_token", rememberToken, cookieOptions);
            httpContext.Response.Cookies.Append("device_id", deviceId, cookieOptions);
        }

        public async Task SignOutAsync()
        {
            // Удаляем remember me токен из базы
            var email = _httpContextAccessor.HttpContext?.Request.Cookies["remember_email"];
            var deviceId = _httpContextAccessor.HttpContext?.Request.Cookies["device_id"];

            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(deviceId))
            {
                await _rememberDeviceService.RemoveRememberTokenAsync(email, deviceId);
            }

            // Очищаем cookies
            _httpContextAccessor.HttpContext?.Response.Cookies.Delete("remember_email");
            _httpContextAccessor.HttpContext?.Response.Cookies.Delete("remember_token");
            _httpContextAccessor.HttpContext?.Response.Cookies.Delete("device_id");

            // Выход из системы
            await _httpContextAccessor.HttpContext?.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }


        private string GenerateSecureToken()
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var tokenBytes = new byte[32];
            rng.GetBytes(tokenBytes);
            return Convert.ToBase64String(tokenBytes);
        }
    }
}
