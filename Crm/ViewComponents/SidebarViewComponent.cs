using System.Security.Claims;
using Crm.Entity.Services;
using Crm.Navigation;
using Crm.Services.Workspace;
using Microsoft.AspNetCore.Mvc;

namespace Crm.ViewComponents
{
    /// <summary>
    /// Левое меню приложения: блок компании (с переключением), разделы из NavMenu
    /// для текущего режима, переключатель режима (если пользователю можно) и ссылки внизу.
    /// </summary>
    public class SidebarViewComponent : ViewComponent
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly IUserRepository _userRepository;
        private readonly IWorkspaceService _workspace;
        private readonly ILogger<SidebarViewComponent> _logger;

        public SidebarViewComponent(
            ICompanyRepository companyRepository,
            IUserRepository userRepository,
            IWorkspaceService workspace,
            ILogger<SidebarViewComponent> logger)
        {
            _companyRepository = companyRepository;
            _userRepository = userRepository;
            _workspace = workspace;
            _logger = logger;
        }

        /// <param name="mode">Режим, если layout его уже получил (чтобы не запрашивать повторно).</param>
        public async Task<IViewComponentResult> InvokeAsync(string? mode = null)
        {
            mode = WorkspaceMode.Normalize(mode ?? await _workspace.GetCurrentModeAsync());

            var model = new SidebarViewModel
            {
                Mode = mode,
                Groups = NavMenu.For(mode),
                ActiveController = ViewContext.RouteData.Values["controller"]?.ToString() ?? string.Empty,
            };

            try
            {
                model.CanChangeMode = await _workspace.CanChangeModeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sidebar: не удалось проверить право смены режима");
            }

            var claim = (User as ClaimsPrincipal)?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(claim, out var userId))
            {
                try
                {
                    await FillCompanyAsync(model, userId);
                }
                catch (Exception ex)
                {
                    // Сайдбар не должен ронять страницу, если что-то не так с данными компании
                    _logger.LogWarning(ex, "Sidebar: не удалось получить данные компании пользователя {UserId}", userId);
                }
            }

            return View(model);
        }

        private async Task FillCompanyAsync(SidebarViewModel model, int userId)
        {
            var user = _userRepository.GetUserById(userId)?.Data;

            var memberships = _companyRepository.GetUserActiveCompanies(userId) ?? new();
            model.Companies = memberships
                .Where(cu => cu.Company != null && cu.Company.IsActive != false)
                .GroupBy(cu => cu.CompanyId)
                .Select(g => new SidebarCompany(g.Key, g.First().Company.Name))
                .ToList();

            int? currentId = user?.CurrentCompanyId;
            if (currentId == null && model.Companies.Count > 0)
                currentId = model.Companies[0].Id;
            if (currentId == null)
                return;

            model.CurrentCompanyId = currentId;
            var company = await _companyRepository.GetCompanyByIdAsync(currentId.Value);
            model.CompanyName = company?.Name
                ?? model.Companies.FirstOrDefault(c => c.Id == currentId)?.Name;

            if (company != null)
            {
                var users = await _companyRepository.GetCompanyUsersAsync(company.Id);
                model.MemberCount = users?.Count;
            }
        }
    }

    public record SidebarCompany(int Id, string Name);

    public class SidebarViewModel
    {
        public string Mode { get; set; } = WorkspaceMode.Full;
        public bool IsTasksMode => Mode == WorkspaceMode.Tasks;
        public bool CanChangeMode { get; set; }
        public IReadOnlyList<NavGroup> Groups { get; set; } = Array.Empty<NavGroup>();
        public string ActiveController { get; set; } = string.Empty;

        public string? CompanyName { get; set; }
        public int? CurrentCompanyId { get; set; }
        public int? MemberCount { get; set; }
        public List<SidebarCompany> Companies { get; set; } = new();

        public bool IsActive(NavItem item) =>
            !string.IsNullOrEmpty(ActiveController)
            && item.Controllers.Any(c => string.Equals(c, ActiveController, StringComparison.OrdinalIgnoreCase));

        /// <summary>Первая буква/цифра названия для квадратного логотипа.</summary>
        public static string Initial(string? name)
        {
            var ch = (name ?? string.Empty).FirstOrDefault(char.IsLetterOrDigit);
            return ch == default ? "К" : char.ToUpperInvariant(ch).ToString();
        }

        /// <summary>«14 человек», «2 человека», «1 человек».</summary>
        public static string People(int n)
        {
            var mod100 = n % 100;
            var mod10 = n % 10;
            var word = (mod100 is >= 11 and <= 14) ? "человек"
                : mod10 is >= 2 and <= 4 ? "человека"
                : "человек";
            return $"{n} {word}";
        }
    }
}
