using Crm.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Crm.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public ProjectsController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        } 
    }
}
