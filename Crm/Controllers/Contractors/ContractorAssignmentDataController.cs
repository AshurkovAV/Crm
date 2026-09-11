using Crm.Entity.Services;
using Crm.Models.Contractors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Crm.Controllers;

/// <summary>
/// Назначение производственного этапа внешнему подрядчику и генерация Magic Link (Модуль В ТЗ).
/// Используется внутренним менеджером/цехом — требует авторизации и ограничен текущей компанией.
/// Сама сгенерированная ссылка на подрядчика (см. ContractorPortalController) — уже анонимна.
/// </summary>
[Authorize]
[Route("api/v1/contractor-assignment")]
public class ContractorAssignmentDataController : Controller
{
    private readonly IContractorAssignmentRepository _assignmentRepository;

    public ContractorAssignmentDataController(IContractorAssignmentRepository assignmentRepository)
    {
        _assignmentRepository = assignmentRepository;
    }

    /// <summary>
    /// Список незавершённых этапов производства текущей компании — для выбора на форме назначения.
    /// </summary>
    [HttpGet("stages")]
    public async Task<IActionResult> GetStages()
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var stages = await _assignmentRepository.GetAssignableStagesAsync(userId.Value);
        return Json(stages);
    }

    /// <summary>
    /// Создаёт Magic Link для подрядчика на конкретный этап.
    /// </summary>
    [HttpPost("generate-link")]
    public async Task<IActionResult> GenerateLink([FromBody] AssignContractorRequest request)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (request.ProductionTaskId <= 0 || request.ContractorId <= 0)
            return BadRequest(new { message = "Не указан этап или подрядчик." });

        var result = await _assignmentRepository.AssignAndCreateLinkAsync(
            request.ProductionTaskId, request.ContractorId, userId.Value, request.ExpiresInDays);

        if (result == null)
            return NotFound(new { message = "Этап или подрядчик не найдены в текущей компании." });

        return Json(new
        {
            token = result.Token,
            url = result.RelativeUrl,
            createdAt = result.CreatedAt,
            expiresAt = result.ExpiresAt
        });
    }

    private int? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
