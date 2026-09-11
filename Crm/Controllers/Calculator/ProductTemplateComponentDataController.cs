using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Claims;

namespace Crm.Controllers;

/// <summary>
/// CRUD строк состава (BOM) шаблона изделия — ProductTemplateComponent. Отображается как вложенный
/// грид на странице шаблона (см. Views/Calculator/Index.cshtml), отфильтрован по productTemplateId.
/// </summary>
[Authorize]
public class ProductTemplateComponentDataController : Controller
{
    private readonly IProductTemplateComponentRepository _repository;

    public ProductTemplateComponentDataController(IProductTemplateComponentRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> Get(DataSourceLoadOptions loadOptions, int productTemplateId)
    {
        var userId = GetUserId();
        var items = userId.HasValue
            ? await _repository.GetByTemplateAsync(productTemplateId, userId.Value)
            : new List<ProductTemplateComponent>();

        var rows = items.Select(item => new
        {
            item.Id,
            item.ProductTemplateId,
            item.ComponentId,
            ComponentName = item.Component.Name,
            ComponentUnit = item.Component.Unit,
            item.QuantityFormula,
            item.Notes
        });
        return Json(DataSourceLoader.Load(rows, loadOptions));
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromForm] BomValues form)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var item = new ProductTemplateComponent { QuantityFormula = "Width * Height * Quantity" };
        JsonConvert.PopulateObject(form.Values ?? "{}", item);
        item.Id = 0;
        item.QuantityFormula = string.IsNullOrWhiteSpace(item.QuantityFormula) ? "Width * Height * Quantity" : item.QuantityFormula.Trim();

        var created = await _repository.AddAsync(item, userId.Value);
        return created == null ? BadRequest(new { message = "Шаблон изделия не найден." }) : StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut]
    public async Task<IActionResult> Put(int key, [FromForm] BomValues form)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var item = await _repository.GetAsync(key, userId.Value);
        if (item == null)
            return NotFound();

        JsonConvert.PopulateObject(form.Values ?? "{}", item);
        item.Id = key;
        item.QuantityFormula = string.IsNullOrWhiteSpace(item.QuantityFormula) ? "Width * Height * Quantity" : item.QuantityFormula.Trim();
        return await _repository.UpdateAsync(item, userId.Value) ? Ok() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        return await _repository.DeleteAsync(id, userId.Value) ? Ok() : NotFound();
    }

    private int? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }

    public sealed class BomValues
    {
        public string? Key { get; set; }
        public string? Values { get; set; }
    }
}
