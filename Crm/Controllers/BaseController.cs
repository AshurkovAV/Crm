using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Crm.Entity.ModelsCrm;
using Crm.Extensions;

namespace Crm.Controllers
{
    public class BaseController : Controller
    {
        protected User CurrentUser => GetCurrentUser();

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Пропускаем проверку для AccountController
            if (IsAccountController())
            {
                base.OnActionExecuting(context);
                return;
            }

            // Проверяем авторизацию
            if (!IsUserAuthenticated())
            {
                context.Result = RedirectToAction("Login", "Account");
                return;
            }

            base.OnActionExecuting(context);
        }

        private bool IsUserAuthenticated()
        {
            try
            {
                return HttpContext.Session.IsUserLoggedIn();
            }
            catch
            {
                return false;
            }
        }

        private User GetCurrentUser()
        {
            return HttpContext.Session.GetCurrentUser();
        }

        private bool IsAccountController()
        {
            var controllerName = ControllerContext.RouteData.Values["controller"]?.ToString();
            return string.Equals(controllerName, "Account", StringComparison.OrdinalIgnoreCase);
        }
    }
}
