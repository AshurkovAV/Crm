using Crm.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Crm.Controllers
{
    [Authorize]
    public class DealsController : Controller
    {
        private readonly ILogger<DealsController> _logger;

        public DealsController(ILogger<DealsController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View(); 
        } 
    }
}
