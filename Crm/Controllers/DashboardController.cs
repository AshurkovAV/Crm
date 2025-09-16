using Microsoft.AspNetCore.Mvc;

namespace Crm.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
