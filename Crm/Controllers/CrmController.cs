using Microsoft.AspNetCore.Mvc;

namespace Crm.Controllers
{
    public class CrmController: Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
