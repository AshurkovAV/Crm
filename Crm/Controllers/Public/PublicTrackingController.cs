using Crm.Entity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Controllers.Public;

/// <summary>
/// Публичный трекер заказа для конечного клиента (Модуль Г ТЗ, "эффект Додо").
/// Доступ анонимный, без какой-либо привязки к текущему пользователю/компании CRM —
/// единственный ключ доступа — Deal.PublicToken (непредсказуемый Guid) из ссылки,
/// которую клиенту присылает менеджер. Числовой Deal.Id здесь нигде не используется.
/// [AllowAnonymous] проставлен явно (хотя в проекте на этот контроллер и так нет
/// глобальной политики авторизации по умолчанию — см. Program.cs), чтобы маршрут
/// оставался открытым даже если такая политика появится позже.
/// </summary>
[AllowAnonymous]
[Route("")]
public class PublicTrackingController : Controller
{
    private readonly IPublicTrackingRepository _publicTrackingRepository;

    public PublicTrackingController(IPublicTrackingRepository publicTrackingRepository)
    {
        _publicTrackingRepository = publicTrackingRepository;
    }

    /// <summary>GET /track/{token} — HTML-страница отслеживания заказа для клиента.</summary>
    [HttpGet("track/{token:guid}")]
    public async Task<IActionResult> Track(Guid token)
    {
        var data = await _publicTrackingRepository.GetByPublicTokenAsync(token);
        if (data == null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("~/Views/Public/NotFound.cshtml");
        }

        return View("~/Views/Public/Track.cshtml", data);
    }

    /// <summary>GET /api/v1/public/track/{token} — JSON с данными для страницы отслеживания.</summary>
    [HttpGet("api/v1/public/track/{token:guid}")]
    public async Task<IActionResult> TrackApi(Guid token)
    {
        var data = await _publicTrackingRepository.GetByPublicTokenAsync(token);
        if (data == null)
        {
            return NotFound(new { message = "Заказ не найден" });
        }

        return Json(data);
    }
}
