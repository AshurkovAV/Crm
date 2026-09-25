using Crm.Entity.Services;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Crm.Application.Features.Accounts.Commands.CreateUser;
using Crm.Entity.ModelsCrm;
using Crm.Application.Features.Accounts.Commands.Login;
using Microsoft.AspNetCore.Authorization;
using Crm.Core.Features.Account.Interfaces;
using Crm.Core.Features.Email.Interfaces;
using System.Security.Claims;


namespace Crm.Controllers
{
    public class AccountController : Controller
    {
        private ICrmRepository  _crmRepository;
        private readonly IMediator _mediator;
        private readonly IRememberDeviceService _rememberDeviceService;
        private readonly IVerificationTokenRepository _tokenRepository;
        private readonly IUserRepository _userRepository;
        private readonly Application.Interfaces.IAuthenticationService _authenticationService;
        public AccountController(
            ICrmRepository                                      crmRepository,
            IRememberDeviceService                              rememberDeviceServicev,
            IVerificationTokenRepository                        verificationTokenRepository,
            IMediator                                           mediator,
            IUserRepository                                     userRepository,
            Application.Interfaces.IAuthenticationService       authenticationService)
        {
            _mediator = mediator;
            _crmRepository = crmRepository;             
            _rememberDeviceService = rememberDeviceServicev;
            _tokenRepository = verificationTokenRepository;
            _userRepository = userRepository;
            _authenticationService = authenticationService;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");
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


        private IActionResult RedirectToLocal(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                // Если returnUrl валидный и локальный - редиректим туда
                return Redirect(returnUrl);
            }
            else
            {
                // Иначе на главную страницу
                return RedirectToAction("Index", "Home");
            }
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
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToLocal();
            }
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
                    var user = _userRepository.GetUser(email);
                    await _authenticationService.AuthenticateWithCookiesAsync(user.Data);

                    return RedirectToAction("Index", "Account");
                }
                else
                {
                    // Очищаем невалидные куки
                    await _authenticationService.ClearRememberTokenAsync(email);
                }
            }

            // Если куков нет или они невалидны - показываем страницу входа
            return View();
        }


        [HttpPost]
        [Route("/Account/Login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand model)
        {
            Console.WriteLine("=== LOGIN PROCESS START ===");

            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToLocal();
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "invalid_credentials" });
            } 

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
            var rememberEmail = Request.Cookies["remember_email"];
            var rememberToken = Request.Cookies["remember_token"];
            var deviceId = Request.Cookies["device_id"]; 
            if (!string.IsNullOrEmpty(rememberEmail) 
                && !string.IsNullOrEmpty(rememberToken) 
                && !string.IsNullOrEmpty(deviceId)
                && rememberEmail == model.Email)
            {
                // Проверяем валидность токена
                var isValid = await _rememberDeviceService.ValidateRememberTokenAsync(rememberEmail, rememberToken, deviceId);

                if (isValid)
                {
                    await _authenticationService.AuthenticateWithCookiesAsync(result.UserBase);
                    return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
                }
                else
                {
                    await _authenticationService.ClearRememberTokenAsync(model.Email);
                }
            }
            else
            {
                if (result.UserBase?.PasswordHash == null)
                {
                    // Создаем токен в базе
                    var token = await _tokenRepository.CreateAsync(model.Email, TimeSpan.FromHours(24));

                    // Формирование URL
                    var setPasswordUrl = Url.Action("SetPassword", "Email", new
                    {
                        token = token.Token,
                        email = model.Email
                    }, protocol: HttpContext.Request.Scheme);
                    return Json(new
                    {
                        success = true,
                        redirectUrl = setPasswordUrl,
                        email = model.Email,
                        message = "Ссылка для установки пароля сгенерирована"
                    });
                    //// Прямой редирект на страницу установки пароля                   
                    //return RedirectToAction("SetPassword", "Email", new
                    //{
                    //    token = token.Token,
                    //    email = token.Email
                    //});

                }
                return Json(new
                {
                    success = true,
                    redirectUrl = Url.Action("Login", "Account"),
                    email = model.Email, // Передаем email для JS
                  //  token = verificationToken
                });
            }
            await _authenticationService.AuthenticateWithCookiesAsync( result.UserBase);

            return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
        }


          
        [Route("/Account/logout")] // Поддерживаем старый URL
        public async Task<IActionResult> Logout()
        {
            await _authenticationService.SignOutAsync();          
            return RedirectToAction("", "Account");
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
                if (request.RememberMe)
                {
                    await _authenticationService.CreateRememberTokenAsync(request.Email, result.User.Id);
                }

                await _authenticationService.AuthenticateWithCookiesAsync(result.User);

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
            Console.WriteLine("/Account/Password");
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "invalid_credentials" });
            }

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


            await _authenticationService.AuthenticateWithCookiesAsync(result.User);

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
            var result = await _mediator.Send(model);

            if (result.Succeeded)
            {              
                await _authenticationService.AuthenticateWithCookiesAsync(result.UserBase);

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
                var user = _userRepository.GetUser(email);
                await _authenticationService.AuthenticateWithCookiesAsync(user.Data);

                // Вместо JSON возвращаем View с JavaScript для закрытия окна
                return View("YandexAuthSuccess", new { Email = email, Name = name });
            }
            catch (Exception ex)
            {                
                return RedirectToAction("Login", new { error = "auth_failed" });
            }
        } 
    }



    public class VerifyPasswordRequest
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
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
