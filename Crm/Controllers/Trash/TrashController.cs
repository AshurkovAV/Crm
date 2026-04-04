using Crm.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Crm.Controllers
{
    [Authorize]
    public class TrashController : Controller
    {
        private readonly ILogger<TrashController> _logger;

        public TrashController(ILogger<TrashController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        } 
    }
}
