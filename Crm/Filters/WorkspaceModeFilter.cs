using Crm.Navigation;
using Crm.Services.Workspace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Crm.Filters
{
    /// <summary>
    /// Глобальный фильтр режима «Только задачи». Если у текущей компании пользователя режим Tasks,
    /// разделы продаж, склада и производства (страницы и их data/API-контроллеры) недоступны:
    /// обычный GET страницы перенаправляется на /Tasks, AJAX/JSON/API-запрос получает 403 с сообщением.
    /// </summary>
    public class WorkspaceModeFilter : IAsyncActionFilter
    {
        public const string BlockedMessage = "Раздел недоступен в режиме «Только задачи»";

        /// <summary>
        /// Data/API-контроллеры скрытых разделов, которых нет в меню (NavMenu знает только страницы).
        /// </summary>
        private static readonly HashSet<string> TasksModeBlockedControllers = new(StringComparer.OrdinalIgnoreCase)
        {
            // Продажи
            "Deals", "DealData", "Crm", "Contacts", "Sales",
            // Калькулятор и номенклатура
            "Calculator", "CalculatorData", "ComponentData", "SupplierData",
            "ProductTemplateData", "ProductTemplateComponentData", "OrderItemData",
            // Склад и производство
            "Warehouse", "ProductionTaskData",
            // Подрядчики (внутренняя часть; публичный портал подрядчика не блокируется)
            "Contractors", "ContractorData", "ContractorAssignmentData",
        };

        /// <summary>
        /// Никогда не блокируются, даже если когда-нибудь попадут в скрытые пункты меню.
        /// </summary>
        private static readonly HashSet<string> AlwaysAllowedControllers = new(StringComparer.OrdinalIgnoreCase)
        {
            "Account", "Invite", "Invitation", "Profile", "Workspace", "Home", "Dashboard",
            "Tasks", "TaskData", "Project", "Projects", "ProjectData", "ProjectUserData",
            "Workflows", "Vault", "VaultData", "Company", "Companies", "UserData",
            "Online", "StatusData", "YandexAuth", "VkAuth", "Email", "SendMail",
            "PublicTracking", "ContractorPortal", "Trash", "CrmSetup", "Test",
        };

        private readonly IWorkspaceService _workspaceService;

        public WorkspaceModeFilter(IWorkspaceService workspaceService)
        {
            _workspaceService = workspaceService;
        }

        public static bool IsBlockedInTasksMode(string? controllerName)
        {
            if (string.IsNullOrEmpty(controllerName) || AlwaysAllowedControllers.Contains(controllerName))
            {
                return false;
            }

            return TasksModeBlockedControllers.Contains(controllerName)
                || NavMenu.HiddenControllers(WorkspaceMode.Tasks).Contains(controllerName);
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            var controllerName = (context.ActionDescriptor as ControllerActionDescriptor)?.ControllerName;

            if (httpContext.User?.Identity?.IsAuthenticated != true || !IsBlockedInTasksMode(controllerName))
            {
                await next();
                return;
            }

            var mode = await _workspaceService.GetCurrentModeAsync();
            if (mode != WorkspaceMode.Tasks)
            {
                await next();
                return;
            }

            if (IsPageNavigation(httpContext.Request))
            {
                context.Result = new RedirectResult("/Tasks");
            }
            else
            {
                context.Result = new JsonResult(new { message = BlockedMessage })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }

        /// <summary>Обычный переход по странице (GET без признаков AJAX/JSON/API).</summary>
        private static bool IsPageNavigation(HttpRequest request)
        {
            if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
            {
                return false;
            }

            if (string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }
    }
}
