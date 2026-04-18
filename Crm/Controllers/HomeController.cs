using Crm.Entity.Services;
using Crm.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace Crm.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private ICompanyRepository _companyRepository;
        public HomeController(
            ICompanyRepository companyRepository,
            ILogger<HomeController> logger)
        {
            _companyRepository = companyRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Проверяем, создавал ли пользователь компанию (является владельцем хотя бы одной компании)
            bool hasOwnCompany = await _companyRepository.UserHasOwnCompanyAsync(int.Parse(userId));
            // Проверяем, состоит ли пользователь в любой компании (включая ту, где он не владелец)
            bool hasAnyCompany = await _companyRepository.UserHasAnyCompanyAsync(int.Parse(userId));

            ViewBag.HasOwnCompany = hasOwnCompany;
            ViewBag.HasAnyCompany = hasAnyCompany;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Bt()
        {
            return View();
        }
        public IActionResult Create()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
