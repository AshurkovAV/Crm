using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using Crm.Models;
using Crm.Models.Home;
using Crm.Navigation;
using Crm.Services;
using Crm.Services.Workspace;
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
        private readonly ICompanyRepository _companyRepository;
        private readonly IDashboardQueries _dashboardQueries;
        private readonly IWorkspaceService _workspaceService;

        public HomeController(
            ICompanyRepository companyRepository,
            IDashboardQueries dashboardQueries,
            IWorkspaceService workspaceService,
            ILogger<HomeController> logger)
        {
            _companyRepository = companyRepository;
            _dashboardQueries = dashboardQueries;
            _workspaceService = workspaceService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            // Является ли пользователь владельцем (создателем) хотя бы одной компании
            bool hasOwnCompany = await _companyRepository.UserHasOwnCompanyAsync(userId);
            // Состоит ли пользователь в какой-либо компании (в т.ч. не своей)
            bool hasAnyCompany = await _companyRepository.UserHasAnyCompanyAsync(userId);

            ViewBag.HasOwnCompany = hasOwnCompany;
            ViewBag.HasAnyCompany = hasAnyCompany;

            var model = new HomeIndexViewModel
            {
                HasOwnCompany = hasOwnCompany,
                HasAnyCompany = hasAnyCompany,
                Now = DateTime.Now
            };

            if (!hasAnyCompany)
                return View(model);

            model.Mode = WorkspaceMode.Normalize(await _workspaceService.GetCurrentModeAsync());
            try
            {
                model.Data = await _dashboardQueries.GetHomeAsync(userId, model.IsFull);
            }
            catch (Exception ex)
            {
                // Дашборд не должен ронять главную: показываем пустые виджеты.
                _logger.LogError(ex, "Не удалось загрузить данные главной для пользователя {UserId}", userId);
            }

            var tone = 0;
            model.Team = model.Data.Team.Select(m =>
            {
                var name = BuildName(m);
                return new HomeTeamMemberView
                {
                    UserId = m.UserId,
                    Name = name,
                    Initials = BuildInitials(m, name),
                    Position = m.Position,
                    AvatarUrl = AvatarStorage.GetUrl(new User { DefaultAvatarId = m.DefaultAvatarId, IsAvatarEmpty = m.IsAvatarEmpty }),
                    IsMe = m.UserId == userId,
                    Tone = tone++ % 5
                };
            }).ToList();

            return View(model);
        }

        private static string BuildName(HomeTeamMember m)
        {
            var full = string.Join(" ", new[] { m.FirstName, m.LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));
            if (!string.IsNullOrWhiteSpace(full)) return full;
            if (!string.IsNullOrWhiteSpace(m.DisplayName)) return m.DisplayName!.Trim();
            if (!string.IsNullOrWhiteSpace(m.Email)) return m.Email!.Trim();
            return "Сотрудник";
        }

        private static string BuildInitials(HomeTeamMember m, string name)
        {
            static string First(string? s) => string.IsNullOrWhiteSpace(s) ? "" : char.ToUpperInvariant(s.Trim()[0]).ToString();
            var initials = First(m.FirstName) + First(m.LastName);
            if (initials.Length == 0)
                initials = string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => First(w)));
            return initials.Length == 0 ? "?" : initials;
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
