using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using Crm.Models.Contractors;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Claims;

namespace Crm.Controllers;

/// <summary>
/// CRUD подрядчиков (Модуль В ТЗ "Внешняя кооперация"). Company-scoped, только для внутренних
/// авторизованных пользователей (менеджер/цех) — не путать с анонимным ContractorPortalController.
/// </summary>
[Authorize]
[Route("[controller]")]
public class ContractorDataController : Controller
{
    private readonly IContractorRepository _contractorRepository;

    public ContractorDataController(IContractorRepository contractorRepository)
    {
        _contractorRepository = contractorRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
    {
        var userId = GetUserId();
        var contractors = userId.HasValue ? await _contractorRepository.GetByUserAsync(userId.Value) : new List<Contractor>();
        return Json(DataSourceLoader.Load(contractors.Select(ToRow), loadOptions));
    }

    /// <summary>
    /// Облегчённый список для выпадающих списков (например, на форме назначения подрядчика).
    /// Отдельный шаблон маршрута, чтобы не конфликтовать с Get (оба GET на уровне контроллера).
    /// </summary>
    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup(DataSourceLoadOptions loadOptions)
    {
        var userId = GetUserId();
        var contractors = (userId.HasValue ? await _contractorRepository.GetByUserAsync(userId.Value) : new List<Contractor>())
            .Where(c => c.IsActive)
            .Select(c => new { c.ContractorId, c.Name });
        return Json(DataSourceLoader.Load(contractors, loadOptions));
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromForm] ContractorValues form)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var contractor = new Contractor
        {
            Name = "Новый подрядчик",
            IsActive = true
        };

        JsonConvert.PopulateObject(form.values ?? "{}", contractor);
        contractor.ContractorId = 0;
        contractor.Name = string.IsNullOrWhiteSpace(contractor.Name) ? "Новый подрядчик" : contractor.Name.Trim();

        await _contractorRepository.AddAsync(contractor, userId.Value);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut]
    public async Task<IActionResult> Put([FromForm] ContractorValues form)
    {
        if (!int.TryParse(form.key, out var key))
            return BadRequest(new { message = "Неверный формат ID." });

        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var contractor = await _contractorRepository.GetAsync(key, userId.Value);
        if (contractor == null)
            return NotFound();

        JsonConvert.PopulateObject(form.values ?? "{}", contractor);
        contractor.ContractorId = key;
        contractor.Name = string.IsNullOrWhiteSpace(contractor.Name) ? "Новый подрядчик" : contractor.Name.Trim();

        return await _contractorRepository.UpdateAsync(contractor, userId.Value) ? Ok() : NotFound();
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromForm] DeleteRequest request)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (!request.key.HasValue)
            return BadRequest(new { message = "ID подрядчика не указан." });

        return await _contractorRepository.DeleteAsync(request.key.Value, userId.Value) ? Ok() : NotFound();
    }

    private static ContractorRow ToRow(Contractor contractor) => new()
    {
        ContractorId = contractor.ContractorId,
        Name = contractor.Name,
        Phone = contractor.Phone,
        Email = contractor.Email,
        Notes = contractor.Notes,
        IsActive = contractor.IsActive
    };

    private int? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
