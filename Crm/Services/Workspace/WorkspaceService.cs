using System.Security.Claims;
using Crm.Entity.Services;
using Crm.Navigation;

namespace Crm.Services.Workspace
{
    /// <summary>
    /// Режим работы текущей компании пользователя. Режим хранится у компании (Company.WorkspaceMode),
    /// поэтому переключение текущей компании переключает и режим.
    /// Результат кэшируется на время запроса (HttpContext.Items): его спрашивают layout, сайдбар и фильтр.
    /// </summary>
    public class WorkspaceService : IWorkspaceService
    {
        private const string CacheKey = "__Crm.WorkspaceState";

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserRepository _userRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly ILogger<WorkspaceService> _logger;

        public WorkspaceService(
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository,
            ICompanyRepository companyRepository,
            ILogger<WorkspaceService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _userRepository = userRepository;
            _companyRepository = companyRepository;
            _logger = logger;
        }

        private sealed record WorkspaceState(int? UserId, int? CompanyId, string Mode, bool CanChange);

        private static readonly WorkspaceState Empty = new(null, null, WorkspaceMode.Full, false);

        public async Task<string> GetCurrentModeAsync() => (await GetStateAsync()).Mode;

        public async Task<bool> CanChangeModeAsync() => (await GetStateAsync()).CanChange;

        public async Task<bool> SetCurrentModeAsync(string mode)
        {
            if (!WorkspaceMode.IsValid(mode))
            {
                return false;
            }

            var state = await GetStateAsync();
            if (state.CompanyId is not int companyId || !state.CanChange)
            {
                return false;
            }

            var saved = await _companyRepository.SetWorkspaceModeAsync(companyId, mode);
            if (saved)
            {
                // Обновляем кэш запроса, чтобы последующие вызовы видели новый режим.
                var context = _httpContextAccessor.HttpContext;
                if (context != null)
                {
                    context.Items[CacheKey] = state with { Mode = mode };
                }
            }

            return saved;
        }

        private async Task<WorkspaceState> GetStateAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                return Empty;
            }

            if (context.Items.TryGetValue(CacheKey, out var cached) && cached is WorkspaceState cachedState)
            {
                return cachedState;
            }

            var state = await LoadStateAsync(context);
            context.Items[CacheKey] = state;
            return state;
        }

        private async Task<WorkspaceState> LoadStateAsync(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated != true
                || !int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Empty;
            }

            try
            {
                var userResult = _userRepository.GetUserById(userId);
                var user = userResult.Success ? userResult.Data : null;
                if (user?.CurrentCompanyId is not int companyId)
                {
                    return Empty with { UserId = userId };
                }

                var company = await _companyRepository.GetCompanyByIdAsync(companyId);
                if (company == null)
                {
                    return Empty with { UserId = userId };
                }

                var canChange = company.OwnerId == userId;
                if (!canChange)
                {
                    var membership = await _companyRepository.GetMembershipAsync(userId, companyId);
                    canChange = membership is { IsActive: true }
                        && string.Equals(membership.Role, "Admin", StringComparison.OrdinalIgnoreCase);
                }

                return new WorkspaceState(userId, companyId, WorkspaceMode.Normalize(company.WorkspaceMode), canChange);
            }
            catch (Exception exception)
            {
                // Режим не должен ронять страницу: при ошибке БД работаем как «Полная CRM».
                _logger.LogWarning(exception, "Не удалось определить режим работы компании для пользователя {UserId}", userId);
                return Empty with { UserId = userId };
            }
        }
    }
}
