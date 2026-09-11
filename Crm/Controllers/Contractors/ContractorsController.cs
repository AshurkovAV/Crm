using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Controllers
{
    [Authorize]
    public class ContractorsController : Controller
    {
        public IActionResult Index() => View();

        /// <summary>
        /// Страница назначения производственного этапа подрядчику и генерации Magic Link.
        /// </summary>
        public IActionResult Assign() => View();
    }
}
