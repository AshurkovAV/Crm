using Crm.Navigation;
using Crm.Services.Workspace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Controllers
{
    /// <summary>
    /// Режим работы текущей компании («Полная CRM» / «Только задачи»).
    /// Сайдбар шлёт обычный fetch с JSON без антифорджери-токена; кука авторизации SameSite=Lax,
    /// а тело — application/json (кросс-сайтовая форма его не отправит), поэтому токен не требуем.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/workspace")]
    [IgnoreAntiforgeryToken]
    public class WorkspaceController : ControllerBase
    {
        private readonly IWorkspaceService _workspaceService;

        public WorkspaceController(IWorkspaceService workspaceService)
        {
            _workspaceService = workspaceService;
        }

        public sealed class SetModeRequest
        {
            public string? Mode { get; set; }
        }

        [HttpGet("mode")]
        public async Task<IActionResult> GetMode()
        {
            var mode = await _workspaceService.GetCurrentModeAsync();
            var canChange = await _workspaceService.CanChangeModeAsync();
            return Ok(new { mode, canChange });
        }

        [HttpPost("mode")]
        [Consumes("application/json")]
        public async Task<IActionResult> SetMode([FromBody] SetModeRequest? request)
        {
            var currentMode = await _workspaceService.GetCurrentModeAsync();

            if (request == null || !WorkspaceMode.IsValid(request.Mode))
            {
                return BadRequest(new { success = false, mode = currentMode, message = "Неизвестный режим работы" });
            }

            if (!await _workspaceService.CanChangeModeAsync())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    mode = currentMode,
                    message = "Менять режим работы может только владелец или администратор компании"
                });
            }

            if (!await _workspaceService.SetCurrentModeAsync(request.Mode!))
            {
                return BadRequest(new { success = false, mode = currentMode, message = "Не удалось сменить режим работы" });
            }

            return Ok(new { success = true, mode = request.Mode });
        }
    }
}
