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

namespace Crm.Controllers
{
    public class AccountController : Controller
    {
        private ICrmRepository  _crmRepository;
        private readonly IMediator _mediator;
        public AccountController(ICrmRepository crmRepository,
            IMediator mediator)
        {
            _mediator = mediator;
            _crmRepository = crmRepository;             
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
        public IActionResult ValidateEmail()
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
                    redirectUrl = Url.Action("ValidateEmail", "Account"),
                    email = model.Email, // Передаем email для JS
                    token = verificationToken
                });
            }

            if (result.Succeeded == false)
            {               
                return Json(new { success = false, message = "not_email" });
            }

            await Authenticate(model.Email); // Аутентификация (если нужно)
            HttpContext.Session.SetCurrentUser(result.UserBase);

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
    }
}
