using Crm.Entity.Services;
using Crm.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Crm.Extensions;

namespace Crm.Controllers
{
    public class AuthorizationController : Controller
    {
        private ICrmRepository  _crmRepository;
        public AuthorizationController(ICrmRepository crmRepository)
        {
            _crmRepository = crmRepository;             
        }

        [HttpPost]
        [Route("/Authorization/Login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            Console.WriteLine("/Authorization/Login");
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "invalid_credentials" });
            }
            Console.WriteLine("Запрос в базу данных для проверки пользователя");
            var user = _crmRepository.GetUser(model.Email);
            if (user.HasError)
            {
                return Json(new { success = false, message = "not_email" });
            }

            await Authenticate(model.Email); // Аутентификация (если нужно)
            HttpContext.Session.SetCurrentUser(user.Data);

            return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });

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
