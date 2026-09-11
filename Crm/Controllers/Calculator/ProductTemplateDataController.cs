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
/// CRUD шаблонов изделий (ProductTemplate) — справочник Модуля А ТЗ ("Умный калькулятор").
/// FormulaExpression — строка-формула NCalc, см. CalculationService.
/// </summary>
[Authorize]
public class ProductTemplateDataController : Controller
{
    private readonly IProductTemplateRepository _productTemplateRepository;

    public ProductTemplateDataController(IProductTemplateRepository productTemplateRepository)
    {
        _productTemplateRepository = productTemplateRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
    {
        var userId = GetUserId();
        var templates = userId.HasValue ? await _productTemplateRepository.GetByUserAsync(userId.Value) : new List<ProductTemplate>();
        return Json(DataSourceLoader.Load(templates, loadOptions));
    }

    [HttpGet]
    public async Task<IActionResult> Lookup(DataSourceLoadOptions loadOptions)
    {
        var userId = GetUserId();
        var templates = (userId.HasValue ? await _productTemplateRepository.GetByUserAsync(userId.Value) : new List<ProductTemplate>())
            .Where(t => t.IsActive)
            .Select(t => new { t.ProductTemplateId, t.Name, t.Unit, t.DefaultMarginPercent });
        return Json(DataSourceLoader.Load(templates, loadOptions));
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromForm] ProductTemplateValues form)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var template = new ProductTemplate { Name = "Новый шаблон", Unit = "шт", FormulaExpression = "Width * Height", IsActive = true };
        JsonConvert.PopulateObject(form.Values ?? "{}", template);
        template.ProductTemplateId = 0;
        template.Name = string.IsNullOrWhiteSpace(template.Name) ? "Новый шаблон" : template.Name.Trim();
        template.Unit = string.IsNullOrWhiteSpace(template.Unit) ? "шт" : template.Unit.Trim();
        template.FormulaExpression = string.IsNullOrWhiteSpace(template.FormulaExpression) ? "Width * Height" : template.FormulaExpression.Trim();
        await _productTemplateRepository.AddAsync(template, userId.Value);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut]
    public async Task<IActionResult> Put(int key, [FromForm] ProductTemplateValues form)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var template = await _productTemplateRepository.GetAsync(key, userId.Value);
        if (template == null)
            return NotFound();

        JsonConvert.PopulateObject(form.Values ?? "{}", template);
        template.ProductTemplateId = key;
        template.Name = string.IsNullOrWhiteSpace(template.Name) ? "Новый шаблон" : template.Name.Trim();
        template.FormulaExpression = string.IsNullOrWhiteSpace(template.FormulaExpression) ? "Width * Height" : template.FormulaExpression.Trim();
        return await _productTemplateRepository.UpdateAsync(template, userId.Value) ? Ok() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        return await _productTemplateRepository.DeleteAsync(id, userId.Value) ? Ok() : NotFound();
    }

    private int? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }

    public sealed class ProductTemplateValues
    {
        public string? Key { get; set; }
        public string? Values { get; set; }
    }
}
