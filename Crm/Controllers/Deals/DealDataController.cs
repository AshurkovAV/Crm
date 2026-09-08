using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using Crm.Models.Deals;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;

namespace Crm.Controllers;

[Authorize]
[Route("[controller]")]
public class DealDataController : Controller
{
    private readonly IDealRepository _dealRepository;
    private readonly IClientRepository _clientRepository;

    public DealDataController(IDealRepository dealRepository, IClientRepository clientRepository)
    {
        _dealRepository = dealRepository;
        _clientRepository = clientRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Json(DataSourceLoader.Load(new List<DealRow>(), loadOptions));

        var deals = await _dealRepository.GetByUserAsync(userId.Value);
        return Json(DataSourceLoader.Load(deals.Select(ToRow), loadOptions));
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromForm] DealValues form)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var companyId = await GetCurrentCompanyIdAsync(userId.Value);
        if (!companyId.HasValue)
            return BadRequest(new { message = "Сначала выберите текущую компанию." });

        var deal = new Deal
        {
            CompanyId = companyId.Value,
            OwnerId = userId.Value,
            Title = "Новая сделка",
            Status = "Новая",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        await ApplyValuesAsync(deal, form.values);
        ApplyClientIdFromForm(deal, Request.Form);
        deal.Id = 0;
        deal.CompanyId = companyId.Value;
        deal.OwnerId = userId.Value;
        deal.CreatedDate = DateTime.UtcNow;
        deal.ModifiedDate = DateTime.UtcNow;
        deal.Title = string.IsNullOrWhiteSpace(deal.Title) ? "Новая сделка" : deal.Title.Trim();
        deal.Status = string.IsNullOrWhiteSpace(deal.Status) ? "Новая" : deal.Status;

        await ApplyClientNameAsync(deal);
        await _dealRepository.AddAsync(deal);

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut]
    public async Task<IActionResult> Put([FromForm] DealValues form)
    {
        if (!int.TryParse(form.key, out var key))
            return BadRequest(new { message = "Неверный формат ID." });

        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var deal = await _dealRepository.GetAsync(key, userId.Value);
        if (deal == null)
            return NotFound();

        await ApplyValuesAsync(deal, form.values);
        ApplyClientIdFromForm(deal, Request.Form);
        deal.Id = key;
        deal.ModifiedDate = DateTime.UtcNow;
        deal.Title = string.IsNullOrWhiteSpace(deal.Title) ? "Новая сделка" : deal.Title.Trim();

        await ApplyClientNameAsync(deal);
        return await _dealRepository.UpdateAsync(deal, userId.Value)
            ? Ok()
            : NotFound();
    }

    private async Task ApplyValuesAsync(Deal deal, string? rawValues)
    {
        if (string.IsNullOrWhiteSpace(rawValues))
            return;

        JsonConvert.PopulateObject(rawValues, deal);

        var payload = JsonConvert.DeserializeObject<Dictionary<string, object?>>(rawValues);
        if (payload == null || payload.Count == 0)
            return;

        foreach (var key in new[] { "ClientId", "clientId", "ClientID" })
        {
            if (!payload.TryGetValue(key, out var clientIdValue))
                continue;

            if (clientIdValue is null or "")
            {
                deal.ClientId = null;
                return;
            }

            if (clientIdValue is string s && int.TryParse(s, out var parsed))
            {
                deal.ClientId = parsed > 0 ? parsed : null;
                return;
            }

            if (clientIdValue is int i)
            {
                deal.ClientId = i > 0 ? i : null;
                return;
            }

            if (clientIdValue is long l)
            {
                deal.ClientId = l > 0 ? (int)l : null;
                return;
            }
        }
    }

    private static void ApplyClientIdFromForm(Deal deal, IFormCollection form)
    {
        foreach (var key in new[] { "ClientId", "clientId", "ClientID" })
        {
            if (!form.TryGetValue(key, out var values) || values.Count == 0)
                continue;

            var rawValue = values[0];
            if (string.IsNullOrWhiteSpace(rawValue) || rawValue == "null")
            {
                deal.ClientId = null;
                return;
            }

            if (int.TryParse(rawValue, out var parsed))
            {
                deal.ClientId = parsed > 0 ? parsed : null;
                return;
            }
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromForm] DeleteRequest request)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (!request.key.HasValue)
            return BadRequest(new { message = "ID сделки не указан." });

        return await _dealRepository.DeleteAsync(request.key.Value, userId.Value)
            ? Ok()
            : NotFound();
    }


    private int? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }

    private static async Task<int?> GetCurrentCompanyIdAsync(int userId)
    {
        await using var db = new CrmContext();
        return await db.Users
            .Where(user => user.Id == userId)
            .Select(user => user.CurrentCompanyId)
            .FirstOrDefaultAsync();
    }

    private static DealRow ToRow(Deal deal) => new()
    {
        Id = deal.Id,
        Title = deal.Title,
        ClientId = deal.ClientId,
        ClientName = deal.Client?.Name ?? deal.ClientName,
        Amount = deal.Amount,
        Status = deal.Status,
        ExpectedCloseDate = deal.ExpectedCloseDate,
        Description = deal.Description,
        CreatedDate = deal.CreatedDate,
        ModifiedDate = deal.ModifiedDate
    };

    private async Task ApplyClientNameAsync(Deal deal)
    {
        if (deal.ClientId.HasValue)
        {
            var client = await _clientRepository.GetAsync(deal.ClientId.Value);
            deal.ClientName = client?.Name;
        }
        else
        {
            deal.ClientName = null;
        }
    }
   
}
