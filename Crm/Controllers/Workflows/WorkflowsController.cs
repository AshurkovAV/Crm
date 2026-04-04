using Crm.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Crm.Controllers
{
    [Authorize]
    public class WorkflowsController : Controller
    {
        private readonly ILogger<WorkflowsController> _logger;

        public WorkflowsController(ILogger<WorkflowsController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        } 
    }
}
