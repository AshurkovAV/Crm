using Crm.Entity.DTO;
using Crm.Entity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Crm.Controllers;

/// <summary>
/// Модуль А ТЗ: "Умный калькулятор". POST api/v1/calculations/compute — расчёт сметы
/// (себестоимость/маржа/цена + список материалов с предупреждениями по остаткам на складе).
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/calculations")]
public class CalculatorDataController : ControllerBase
{
    private readonly ICalculationService _calculationService;

    public CalculatorDataController(ICalculationService calculationService)
    {
        _calculationService = calculationService;
    }

    [HttpPost("compute")]
    public async Task<ActionResult<CalculationResultDto>> Compute([FromBody] CalculationRequestDto request)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _calculationService.ComputeAsync(request, userId.Value);
        if (result.HasError)
            return BadRequest(new { message = string.Join(" ", result.Errors) });

        return Ok(result.Data);
    }

    private int? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
