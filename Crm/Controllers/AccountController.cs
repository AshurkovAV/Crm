using Crm.Entity.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Crm.Extensions;
using MediatR;
using Crm.Application.Features.Accounts.Commands.CreateUser;
using Crm.Entity.ModelsCrm;
using Crm.Application.Features.Accounts.Commands.Login;
using Microsoft.AspNetCore.Authorization;
using Crm.Core.Features.Account.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Crm.Controllers
{
    public class AccountController : Controller
    {
        private ICrmRepository  _crmRepository;
        private readonly IMediator _mediator;
        private readonly IRememberDeviceService _rememberDeviceService;
        public AccountController(ICrmRepository crmRepository,
            IRememberDeviceService rememberDeviceServicev,
            IMediator mediator)
        {
            _mediator = mediator;
            _crmRepository = crmRepository;             
            _rememberDeviceService = rememberDeviceServicev;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult VkidPopup()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }
        [AllowAnonymous]
        public IActionResult EmailConfirmationPending()
        {
            // Проверяем токен из cookie
            var tokenFromCookie = Request.Cookies["verification_token"];
            var tokenFromTempData = TempData["VerificationToken"];

            if (string.IsNullOrEmpty(tokenFromCookie) || tokenFromCookie != tokenFromTempData?.ToString())
            {
                // Если токен не valid, перенаправляем на логин
                return RedirectToAction("Index");
            }

            // Показываем страницу
            ViewBag.Email = TempData["VerificationEmail"];
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Login()
        {
            // Проверяем, есть ли данные в куках для автоматического входа
            var email = Request.Cookies["remember_email"];
            var rememberToken = Request.Cookies["remember_token"];
            var deviceId = Request.Cookies["device_id"];

            if (!string.IsNullOrEmpty(email) &&
                !string.IsNullOrEmpty(rememberToken) &&
                !string.IsNullOrEmpty(deviceId))
            {
                // Проверяем валидность remember token
                var isValid = await _rememberDeviceService.ValidateRememberTokenAsync(email, rememberToken, deviceId);

                if (isValid)
                {
                    await Authenticate(email); // Аутентификация (если нужно)
                   // HttpContext.Session.SetCurrentUser(result.UserBase);

                    return RedirectToAction("Index", "Account");
                }
                else
                {
                    // Очищаем невалидные куки
                    ClearRememberCookies();
                }
            }

            // Если куков нет или они невалидны - показываем страницу входа
            return View();
        }


        [HttpPost]
        [Route("/Account/Login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand model)
        {
            Console.WriteLine("/Account/Login");
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "invalid_credentials" });
            }            

            Console.WriteLine("Запрос в базу данных для проверки пользователя");

            var result = await _mediator.Send(model);
          
            if (result.Succeeded == false && result.Errors[0] == "Пользователь не подтвердил email")
            {
                // Генерируем временный токен
                var verificationToken = Guid.NewGuid().ToString();

                // Сохраняем в TempData или кэше
                TempData["VerificationToken"] = verificationToken;
                TempData["VerificationEmail"] = model.Email;

                // Устанавливаем cookie с токеном
                Response.Cookies.Append("verification_token", verificationToken, new CookieOptions
                {
                    Expires = DateTime.Now.AddMinutes(5),
                    HttpOnly = true,
                    Secure = true
                });

                // Сохраняем email для использования на странице верификации
                TempData["Email"] = model.Email;

                return Json(new
                {
                    success = true,
                    redirectUrl = Url.Action("EmailConfirmationPending", "Account"),
                    email = model.Email, // Передаем email для JS
                    token = verificationToken
                });
            }

            if (result.Succeeded == false)
            {               
                return Json(new { success = false, message = "not_email" });
            }

            // Проверяем remember cookie
            var email = Request.Cookies["remember_email"];
            var rememberToken = Request.Cookies["remember_token"];
            var deviceId = Request.Cookies["device_id"]; 
            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(rememberToken) && !string.IsNullOrEmpty(deviceId))
            {
                // Проверяем валидность токена
                var isValid = await _rememberDeviceService.ValidateRememberTokenAsync(email, rememberToken, deviceId);

                if (isValid)
                {
                    await Authenticate(model.Email); // Аутентификация 
                    HttpContext.Session.SetCurrentUser(result.UserBase);
                    return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
                }
                else
                {
                    // Очищаем невалидные cookie
                    ClearRememberCookies();
                }
            }
            else
            {
                return Json(new
                {
                    success = true,
                    redirectUrl = Url.Action("login", "Account"),
                    email = model.Email, // Передаем email для JS
                  //  token = verificationToken
                });
            }
            await Authenticate(model.Email); // Аутентификация (если нужно)
            HttpContext.Session.SetCurrentUser(result.UserBase);

            return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
        }

        [HttpPost]
        [Route("/Account/VerifyPassword")]
        public async Task<ActionResult<VerifyPasswordResponse>> VerifyPassword([FromBody] VerifyPasswordRequest request)
        {
            try
            {  
                var command = new VerifyPasswordCommand
                {
                    UserId = request.UserId,
                    Email = request.Email,
                    Password = request.Password
                };

                var result = await _mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Json(new { success = false, message = "invalid_credentials" });
                }


                await Authenticate(request.Email); // Аутентификация (если нужно)
                HttpContext.Session.SetCurrentUser(result.User);

                return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
            }
            catch (Exception ex)
            {                
                return StatusCode(500, VerifyPasswordResponse.Error("An error occurred during authentication"));
            }
        }


        [HttpPost]
        [Route("/Account/Password")]
        public async Task<IActionResult> Password([FromBody] VerifyPasswordCommand model)
        {
            Console.WriteLine("/Account/Login");
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "invalid_credentials" });
            }

            Console.WriteLine("Запрос в базу данных для проверки пользователя");

            var result = await _mediator.Send(model);

            if (result.Succeeded == false && result.Errors[0] == "Пользователь не подтвердил email")
            {
                // Генерируем временный токен
                var verificationToken = Guid.NewGuid().ToString();

                // Сохраняем в TempData или кэше
                TempData["VerificationToken"] = verificationToken;
                TempData["VerificationEmail"] = model.Email;

                // Устанавливаем cookie с токеном
                Response.Cookies.Append("verification_token", verificationToken, new CookieOptions
                {
                    Expires = DateTime.Now.AddMinutes(5),
                    HttpOnly = true,
                    Secure = true
                });

                // Сохраняем email для использования на странице верификации
                TempData["Email"] = model.Email;

                return Json(new
                {
                    success = true,
                    redirectUrl = Url.Action("EmailConfirmationPending", "Account"),
                    email = model.Email, // Передаем email для JS
                    token = verificationToken
                });
            }

            if (result.Succeeded == false)
            {
                return Json(new { success = false, message = "not_email" });
            }

            // Проверяем remember cookie
            var email = Request.Cookies["remember_email"];
            var rememberToken = Request.Cookies["remember_token"];
            var deviceId = Request.Cookies["device_id"];
            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(rememberToken) && !string.IsNullOrEmpty(deviceId))
            {
                // Проверяем валидность токена
                var isValid = await _rememberDeviceService.ValidateRememberTokenAsync(email, rememberToken, deviceId);

                if (isValid)
                {
                    await Authenticate(model.Email); // Аутентификация 
                    HttpContext.Session.SetCurrentUser(result.User);
                    return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
                }
                else
                {
                    // Очищаем невалидные cookie
                    ClearRememberCookies();
                }
            }
            else
            {
                return Json(new
                {
                    success = true,
                    redirectUrl = Url.Action("login", "Account"),
                    email = model.Email, // Передаем email для JS
                                         //  token = verificationToken
                });
            }



            await Authenticate(model.Email); // Аутентификация (если нужно)
            HttpContext.Session.SetCurrentUser(result.User);

            return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });

        }


        [HttpPost]
        [Route("/Account/Create")]
        public async Task<IActionResult> Create([FromBody] CreateUserCommand model)
        {
            Console.WriteLine("/Account/Create");
            if (!ModelState.IsValid)
            {
                return Json(new { Succeeded = false, message = "invalid_credentials" });
            }
            Console.WriteLine("Запрос в базу данных для проверки пользователя");            
            var result = await _mediator.Send(model);

            if (result.Succeeded)
            {
                await Authenticate(model.Email); // Аутентификация (если нужно)
                HttpContext.Session.SetCurrentUser(result.UserBase);

                return Json(new { Succeeded = true, redirectUrl = Url.Action("start", "CrmSetup") });
            }

            return Json(new { Succeeded = false, message = result.Errors });

        }

        
        [HttpGet("/Account/YandexAuthSuccess")]
        public async Task<IActionResult> YandexAuthSuccess(string token, string email, string name)
        {
            try
            {
                var viewModel = new User
                {
                    DefaultEmail = email,
                    FirstName = name
                };

                await Authenticate(email);
                HttpContext.Session.SetCurrentUser(viewModel);

                // Вместо JSON возвращаем View с JavaScript для закрытия окна
                return View("YandexAuthSuccess", new { Email = email, Name = name });


            }
            catch (Exception ex)
            {                
                return RedirectToAction("Login", new { error = "auth_failed" });
            }
        }

        private async Task Authenticate(string userName)
        {
            // создаем один claim
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, userName)
            };
           
            // создаем объект ClaimsIdentity
            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
            // установка аутентификационных куки
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));          
        }

        private void ClearRememberCookies()
        {
            Response.Cookies.Delete("remember_email");
            Response.Cookies.Delete("remember_token");
            Response.Cookies.Delete("device_id");
        }
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }



    public class VerifyPasswordRequest
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class VerifyPasswordResponse
    {
        public bool Succeeded { get; set; }
        public User User { get; set; }
        public string Token { get; set; }
        public List<string> Errors { get; set; }

        public static VerifyPasswordResponse FromResult(VerifyPasswordResult result)
        {
            return new VerifyPasswordResponse
            {
                Succeeded = result.Succeeded,                
                Token = result.Token,
                Errors = result.Errors
            };
        }

        public static VerifyPasswordResponse Error(string error)
        {
            return new VerifyPasswordResponse
            {
                Succeeded = false,
                Errors = new List<string> { error }
            };
        }
    }
}
