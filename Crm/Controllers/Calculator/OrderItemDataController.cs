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
/// CRUD изделий в составе сделки (OrderItem) — результат работы калькулятора попадает сюда.
/// Company-scoping идёт через Deal.CompanyId (см. OrderItemRepository). Список по DealId и создание
/// нового изделия — стабильный контракт, используется модулем цеха для генерации произв. этапов.
/// </summary>
[Authorize]
public class OrderItemDataController : Controller
{
    private readonly IOrderItemRepository _orderItemRepository;

    public OrderItemDataController(IOrderItemRepository orderItemRepository)
    {
        _orderItemRepository = orderItemRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Get(DataSourceLoadOptions loadOptions, int dealId)
    {
        var userId = GetUserId();
        var items = userId.HasValue
            ? await _orderItemRepository.GetByDealAsync(dealId, userId.Value)
            : new List<OrderItem>();

        var rows = items.Select(item => new
        {
            item.OrderItemId,
            item.DealId,
            item.ProductTemplateId,
            ProductTemplateName = item.ProductTemplate?.Name,
            item.Name,
            item.Quantity,
            item.Width,
            item.Height,
            item.Depth,
            item.CostPrice,
            item.MarginPercent,
            item.Price,
            item.Status,
            item.CreatedDate
        });
        return Json(DataSourceLoader.Load(rows, loadOptions));
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromForm] OrderItemValues form)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var orderItem = new OrderItem { Name = "Новое изделие", Quantity = 1, Status = "Новое", CreatedDate = DateTime.UtcNow };
        JsonConvert.PopulateObject(form.Values ?? "{}", orderItem);
        orderItem.OrderItemId = 0;
        orderItem.Name = string.IsNullOrWhiteSpace(orderItem.Name) ? "Новое изделие" : orderItem.Name.Trim();

        var created = await _orderItemRepository.AddAsync(orderItem, userId.Value);
        return created == null ? BadRequest(new { message = "Сделка не найдена или не принадлежит вашей компании." }) : StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut]
    public async Task<IActionResult> Put(int key, [FromForm] OrderItemValues form)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var orderItem = await _orderItemRepository.GetAsync(key, userId.Value);
        if (orderItem == null)
            return NotFound();

        JsonConvert.PopulateObject(form.Values ?? "{}", orderItem);
        orderItem.OrderItemId = key;
        orderItem.Name = string.IsNullOrWhiteSpace(orderItem.Name) ? "Новое изделие" : orderItem.Name.Trim();
        return await _orderItemRepository.UpdateAsync(orderItem, userId.Value) ? Ok() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        return await _orderItemRepository.DeleteAsync(id, userId.Value) ? Ok() : NotFound();
    }

    private int? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }

    public sealed class OrderItemValues
    {
        public string? Key { get; set; }
        public string? Values { get; set; }
    }
}
