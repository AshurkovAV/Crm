using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using Crm.Models.Calculator;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Claims;

namespace Crm.Controllers;

/// <summary>
/// CRUD ТМЦ/склада (Component) для Модуля А ТЗ. Company-scoped через ComponentRepository.
/// </summary>
[Authorize]
public class ComponentDataController : Controller
{
    private readonly IComponentRepository _componentRepository;

    public ComponentDataController(IComponentRepository componentRepository)
    {
        _componentRepository = componentRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
    {
        var userId = GetUserId();
        var components = userId.HasValue ? await _componentRepository.GetByUserAsync(userId.Value) : new List<Component>();
        var rows = components.Select(c => new ComponentRow
        {
            ComponentId = c.ComponentId,
            Name = c.Name,
            Unit = c.Unit,
            CostPrice = c.CostPrice,
            StockQuantity = c.StockQuantity,
            ReorderLevel = c.ReorderLevel,
            SupplierId = c.SupplierId,
            SupplierName = c.Supplier?.Name,
            IsActive = c.IsActive
        });
        return Json(DataSourceLoader.Load(rows, loadOptions));
    }

    [HttpGet]
    public async Task<IActionResult> Lookup(DataSourceLoadOptions loadOptions)
    {
        var userId = GetUserId();
        var components = (userId.HasValue ? await _componentRepository.GetByUserAsync(userId.Value) : new List<Component>())
            .Where(c => c.IsActive)
            .Select(c => new { c.ComponentId, c.Name, c.Unit });
        return Json(DataSourceLoader.Load(components, loadOptions));
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromForm] ComponentValues form)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var component = new Component { Name = "Новый материал", Unit = "шт", IsActive = true };
        JsonConvert.PopulateObject(form.Values ?? "{}", component);
        component.ComponentId = 0;
        component.Name = string.IsNullOrWhiteSpace(component.Name) ? "Новый материал" : component.Name.Trim();
        component.Unit = string.IsNullOrWhiteSpace(component.Unit) ? "шт" : component.Unit.Trim();
        await _componentRepository.AddAsync(component, userId.Value);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut]
    public async Task<IActionResult> Put(int key, [FromForm] ComponentValues form)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var component = await _componentRepository.GetAsync(key, userId.Value);
        if (component == null)
            return NotFound();

        JsonConvert.PopulateObject(form.Values ?? "{}", component);
        component.ComponentId = key;
        component.Name = string.IsNullOrWhiteSpace(component.Name) ? "Новый материал" : component.Name.Trim();
        return await _componentRepository.UpdateAsync(component, userId.Value) ? Ok() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        return await _componentRepository.DeleteAsync(id, userId.Value) ? Ok() : NotFound();
    }

    private int? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }

    public sealed class ComponentValues
    {
        public string? Key { get; set; }
        public string? Values { get; set; }
    }
}
