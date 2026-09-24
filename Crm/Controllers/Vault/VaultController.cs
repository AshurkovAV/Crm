using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Controllers
{
    [Authorize]
    public class VaultController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
