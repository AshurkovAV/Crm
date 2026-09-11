using Crm.Entity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Controllers;

/// <summary>
/// Анонимный портал подрядчика (Magic Link, Модуль В ТЗ). Никакого логина, никакого
/// company-scoping через сессию/куки — единственная проверка доступа это сам токен (Guid)
/// в URL. Обязательно [AllowAnonymous]: контроллер не должен требовать аутентификации,
/// даже если где-то в пайплайне появится общая политика авторизации.
/// </summary>
[AllowAnonymous]
public class ContractorPortalController : Controller
{
    private readonly IContractorPortalRepository _portalRepository;

    public ContractorPortalController(IContractorPortalRepository portalRepository)
    {
        _portalRepository = portalRepository;
    }

    /// <summary>
    /// Страница подрядчика: GET /contractor/{token}. Сам чертёж/данные подгружаются с
    /// клиента через JSON API ниже — здесь только статическая обёртка под телефон.
    /// </summary>
    [HttpGet("/contractor/{token}")]
    public IActionResult Portal(string token)
    {
        ViewBag.Token = token;
        return View("Portal");
    }

    /// <summary>
    /// GET /api/v1/external/contractor/{token} — изолированное задание для подрядчика по токену.
    /// </summary>
    [HttpGet("/api/v1/external/contractor/{token}")]
    public async Task<IActionResult> GetTask(string token)
    {
        if (!Guid.TryParse(token, out var guid))
            return NotFound(new { message = "Ссылка недействительна." });

        var result = await _portalRepository.GetByTokenAsync(guid);
        return result.Status switch
        {
            ContractorPortalStatus.NotFound => NotFound(new { message = "Ссылка не найдена." }),
            ContractorPortalStatus.Expired => NotFound(new { message = "Срок действия ссылки истёк." }),
            _ => Json(new
            {
                contractorName = result.ContractorName,
                stageName = result.StageName,
                notes = result.Notes,
                orderItemName = result.OrderItemName,
                dealTitle = result.DealTitle,
                deadline = result.Deadline,
                status = result.TaskStatus,
                startedAt = result.StartedAt,
                completedAt = result.CompletedAt,
                tokenUsed = result.TokenUsed
            })
        };
    }

    /// <summary>
    /// POST /api/v1/external/contractor/{token}/accept — «ПРИНЯЛ В РАБОТУ».
    /// </summary>
    [HttpPost("/api/v1/external/contractor/{token}/accept")]
    public async Task<IActionResult> Accept(string token)
    {
        if (!Guid.TryParse(token, out var guid))
            return NotFound(new { message = "Ссылка недействительна." });

        var result = await _portalRepository.AcceptAsync(guid);
        return ToActionResult(result);
    }

    /// <summary>
    /// POST /api/v1/external/contractor/{token}/ready — «ГОТОВО, ОТГРУЖЕНО».
    /// Ставит внутреннюю задачу водителю на забор детали.
    /// </summary>
    [HttpPost("/api/v1/external/contractor/{token}/ready")]
    public async Task<IActionResult> Ready(string token)
    {
        if (!Guid.TryParse(token, out var guid))
            return NotFound(new { message = "Ссылка недействительна." });

        var result = await _portalRepository.ReadyAsync(guid);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult(ContractorPortalActionResult result) => result.Status switch
    {
        ContractorPortalStatus.NotFound => NotFound(new { message = result.Message }),
        ContractorPortalStatus.Expired => NotFound(new { message = result.Message }),
        _ => Json(new { message = result.Message })
    };
}
