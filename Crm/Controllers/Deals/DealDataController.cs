using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
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

        JsonConvert.PopulateObject(form.values ?? "{}", deal);
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
    public async Task<IActionResult> Put(int key, [FromForm] DealValues form)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var deal = await _dealRepository.GetAsync(key, userId.Value);
        if (deal == null)
            return NotFound();

        JsonConvert.PopulateObject(form.values ?? "{}", deal);
        deal.Id = key;
        deal.ModifiedDate = DateTime.UtcNow;
        deal.Title = string.IsNullOrWhiteSpace(deal.Title) ? "Новая сделка" : deal.Title.Trim();
        await ApplyClientNameAsync(deal);

        return await _dealRepository.UpdateAsync(deal, userId.Value)
            ? Ok()
            : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        return await _dealRepository.DeleteAsync(id, userId.Value)
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

    public sealed class DealValues
    {
        public string? key { get; set; }
        public string? values { get; set; }
    }

    public sealed class DealRow
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public int? ClientId { get; set; }
        public string? ClientName { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? ExpectedCloseDate { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
