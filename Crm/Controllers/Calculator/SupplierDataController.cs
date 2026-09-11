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
/// CRUD поставщиков (Supplier), справочник склада для Модуля А ТЗ. Company-scoped через
/// SupplierRepository (см. ClientRepository/ContactsController — тот же паттерн).
/// </summary>
[Authorize]
public class SupplierDataController : Controller
{
    private readonly ISupplierRepository _supplierRepository;

    public SupplierDataController(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
    {
        var userId = GetUserId();
        var suppliers = userId.HasValue ? await _supplierRepository.GetByUserAsync(userId.Value) : new List<Supplier>();
        return Json(DataSourceLoader.Load(suppliers, loadOptions));
    }

    [HttpGet]
    public async Task<IActionResult> Lookup(DataSourceLoadOptions loadOptions)
    {
        var userId = GetUserId();
        var suppliers = (userId.HasValue ? await _supplierRepository.GetByUserAsync(userId.Value) : new List<Supplier>())
            .Where(supplier => supplier.IsActive)
            .Select(supplier => new { supplier.SupplierId, supplier.Name });
        return Json(DataSourceLoader.Load(suppliers, loadOptions));
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromForm] SupplierValues form)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var supplier = new Supplier { Name = "Новый поставщик", IsActive = true };
        JsonConvert.PopulateObject(form.Values ?? "{}", supplier);
        supplier.SupplierId = 0;
        supplier.Name = string.IsNullOrWhiteSpace(supplier.Name) ? "Новый поставщик" : supplier.Name.Trim();
        await _supplierRepository.AddAsync(supplier, userId.Value);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut]
    public async Task<IActionResult> Put(int key, [FromForm] SupplierValues form)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var supplier = await _supplierRepository.GetAsync(key, userId.Value);
        if (supplier == null)
            return NotFound();

        JsonConvert.PopulateObject(form.Values ?? "{}", supplier);
        supplier.SupplierId = key;
        supplier.Name = string.IsNullOrWhiteSpace(supplier.Name) ? "Новый поставщик" : supplier.Name.Trim();
        return await _supplierRepository.UpdateAsync(supplier, userId.Value) ? Ok() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        return await _supplierRepository.DeleteAsync(id, userId.Value) ? Ok() : NotFound();
    }

    private int? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }

    public sealed class SupplierValues
    {
        public string? Key { get; set; }
        public string? Values { get; set; }
    }
}
