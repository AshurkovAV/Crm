using Crm.Core.Features.Email.Interfaces;
using Crm.Core.Features.Email.Models;
using Crm.Entity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace Crm.Controllers
{    
    public class EmailController : Controller
    {
        private readonly IMailService _mailService;
        private readonly ILogger<EmailController> _logger;
        private readonly IVerificationTokenRepository _tokenRepository;
        private readonly IUserRepository _userRepository;


        public EmailController(IMailService mailService, 
            ILogger<EmailController> logger,
            IVerificationTokenRepository tokenRepository,
            IUserRepository userRepository)
        {
            _mailService = mailService;
            _logger = logger;
            _tokenRepository = tokenRepository;
            _userRepository = userRepository;
        }

        // Метод для инициации отправки верификационного письма
        [HttpPost]
        public async Task<IActionResult> SendVerificationEmail([FromBody] EmailRequest request)
        {
            try
            {
                // Создаем токен в базе
                var token = await _tokenRepository.CreateAsync(request.Email, TimeSpan.FromHours(24));

                // Отправляем письмо
                var result = await _mailService.SendVerificationEmailAsync(request.Email, token.Token);

                if (result)
                {
                    _logger.LogInformation("Верификационное письмо отправлено на {Email}", request.Email);
                    return Json(new { success = true, message = "Письмо отправлено" });
                }
                else
                {
                    return Json(new { success = false, message = "Ошибка отправки письма" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке верификационного письма");
                return Json(new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet]
        public IActionResult InvalidToken()
        {
            return View();
        }

        // Страница для установки пароля
        [HttpGet]
        public async Task<IActionResult> SetPassword(string token, string email)
        {
            // Проверяем валидность токена из базы
            var validToken = await _tokenRepository.GetValidTokenAsync(email, token);

            if (validToken == null)
            {
                _logger.LogWarning("Невалидный токен для email: {Email}", email);
                return RedirectToAction("InvalidToken");
            }

            ViewBag.Token = token;
            ViewBag.Email = email;
            return View();
        }

        // Обработка установки пароля
        [HttpPost]
        public async Task<IActionResult> SetPassword([FromBody] SetPasswordRequest request)
        {
            try
            {
                // Проверяем токен из базы
                var validToken = await _tokenRepository.GetValidTokenAsync(request.Email, request.Token);
                if (validToken == null)
                {
                    return Json(new { success = false, message = "Невалидный или просроченный токен" });
                }

                // Валидация пароля
                if (request.Password != request.ConfirmPassword)
                {
                    return Json(new { success = false, message = "Пароли не совпадают" });
                }

                if (!IsPasswordValid(request.Password))
                {
                    return Json(new { success = false, message = "Пароль не соответствует требованиям" });
                }

                // Устанавливаем пароль пользователю
                var result = await _userRepository.SetPasswordAsync(request.Email, request.Password);

                if (result)
                {
                    // Инвалидируем токен
                    await _tokenRepository.InvalidateTokenAsync(validToken.Id);

                    // Генерируем токен "запомнить меня"
                    var rememberToken = GenerateRememberToken();
                    var deviceId = GenerateDeviceId();

                    // Сохраняем в базу (для валидации при последующих входах)
                    var resultSave =  await _userRepository.SaveRememberTokenAsync(request.Email, rememberToken, deviceId);
                    if (resultSave)
                    {
                        // Устанавливаем secure cookie
                        SetRememberCookie(request.Email, rememberToken, deviceId);

                        _logger.LogInformation("Пароль установлен для пользователя: {Email}", request.Email);

                        return Json(new
                        {
                            success = true,
                            message = "Пароль успешно установлен",
                            redirectUrl = "/Account/Login",
                            rememberToken = rememberToken,
                            deviceId = deviceId
                        });
                    }                   
                }
                return Json(new { success = false, message = "Внутренняя ошибка сервера" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при установке пароля для email: {Email}", request.Email);
                return Json(new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }
        private string GenerateRememberToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var tokenData = new byte[32];
            rng.GetBytes(tokenData);
            return Convert.ToBase64String(tokenData)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        private string GenerateDeviceId()
        {
            return Guid.NewGuid().ToString("N");
        }

        private void SetRememberCookie(string email, string rememberToken, string deviceId)
        {
            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(30), // 30 дней
                HttpOnly = true,
                Secure = true, // Только HTTPS
                SameSite = SameSiteMode.Strict,
                Path = "/"
            };

            Response.Cookies.Append("remember_email", email, cookieOptions);
            Response.Cookies.Append("remember_token", rememberToken, cookieOptions);
            Response.Cookies.Append("device_id", deviceId, cookieOptions);
        }        

        private bool IsPasswordValid(string password)
        {
            // Минимум 10 символов
            if (password.Length < 10) return false;

            // Содержит большие латинские буквы
            if (!password.Any(char.IsUpper)) return false;

            // Содержит цифры
            if (!password.Any(char.IsDigit)) return false;

            return true;
        }

        private void ClearVerificationToken(string email)
        {
            // Очистка токена из базы или временного хранилища
            TempData.Remove("VerificationToken");
            TempData.Remove("VerificationEmail");
            Response.Cookies.Delete("verification_token");
        }


        // Метод для валидации email 
        [AllowAnonymous]
        public IActionResult ValidateEmail(string token, string email)
        {
            // Проверяем токен из query parameters и cookie
            var tokenFromQuery = token;
            var tokenFromCookie = Request.Cookies["verification_token"];
            var storedToken = TempData["VerificationToken"]?.ToString();
            var storedEmail = TempData["VerificationEmail"]?.ToString();

            // Валидация токена
            if (string.IsNullOrEmpty(tokenFromQuery) ||
                string.IsNullOrEmpty(tokenFromCookie) ||
                tokenFromQuery != tokenFromCookie ||
                tokenFromQuery != storedToken ||
                email != storedEmail)
            {
                _logger.LogWarning("Невалидный токен верификации для email: {Email}", email);
                return RedirectToAction("Index", "Home");
            }

            // Проверяем expiration
            if (TempData["VerificationExpiration"] is string expirationStr &&
                DateTime.TryParse(expirationStr, out var expiration) &&
                expiration < DateTime.UtcNow)
            {
                _logger.LogWarning("Токен верификации истек для email: {Email}", email);
                return RedirectToAction("Index", "Home");
            }

            // Если все ок - показываем страницу подтверждения
            ViewBag.Email = email;
            ViewBag.Token = tokenFromQuery;

            return View();
        }

        // Метод для подтверждения email
        [HttpPost]
        public IActionResult ConfirmEmail(string token, string email)
        {
            // Дополнительная проверка и подтверждение email в базе данных
            // ...

            _logger.LogInformation("Email подтвержден: {Email}", email);

            // Очищаем cookie
            Response.Cookies.Delete("verification_token");

            return Json(new { success = true, message = "Email успешно подтвержден" });
        }

        private string GenerateVerificationToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var tokenData = new byte[32];
            rng.GetBytes(tokenData);
            return Convert.ToBase64String(tokenData)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}

