using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using Crm.Models.Vault;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Claims;

namespace Crm.Controllers;

/// <summary>
/// API хранилища паролей. ВАЖНО: пароли хранятся БЕЗ ШИФРОВАНИЯ (сознательное решение
/// заказчика) — доступ к записям ограничивается только на уровне приложения:
/// каждую запись видит её автор и пользователи, которым автор явно открыл доступ
/// (см. VaultRepository/VaultEntryAccess). Редактировать, удалять запись и менять
/// список доступа может только автор.
/// </summary>
[Authorize]
[Route("[controller]")]
public class VaultDataController : Controller
{
    private readonly IVaultRepository _vaultRepository;

    public VaultDataController(IVaultRepository vaultRepository)
    {
        _vaultRepository = vaultRepository;
    }

    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Json(DataSourceLoader.Load(new List<VaultRow>(), loadOptions));

        var entries = await _vaultRepository.GetAccessibleAsync(userId.Value);
        var rows = entries.Select(e => ToRow(e, userId.Value));
        return Json(DataSourceLoader.Load(rows, loadOptions));
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromForm] VaultValues form)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var entry = new VaultEntry { Title = "Новая запись" };
        if (!string.IsNullOrWhiteSpace(form.values))
            JsonConvert.PopulateObject(form.values, entry);

        entry.Title = string.IsNullOrWhiteSpace(entry.Title) ? "Новая запись" : entry.Title.Trim();

        var created = await _vaultRepository.AddAsync(entry, userId.Value);
        if (created == null)
            return BadRequest(new { message = "Сначала выберите текущую компанию." });

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut]
    public async Task<IActionResult> Put([FromForm] VaultValues form)
    {
        if (!int.TryParse(form.key, out var key))
            return BadRequest(new { message = "Неверный формат ID." });

        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var entry = await _vaultRepository.GetAsync(key, userId.Value);
        if (entry == null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(form.values))
            JsonConvert.PopulateObject(form.values, entry);
        entry.Id = key;
        entry.Title = string.IsNullOrWhiteSpace(entry.Title) ? "Новая запись" : entry.Title.Trim();

        return await _vaultRepository.UpdateAsync(entry, userId.Value)
            ? Ok()
            : Forbid();
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromForm] DeleteRequest request)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (!request.key.HasValue)
            return BadRequest(new { message = "ID записи не указан." });

        return await _vaultRepository.DeleteAsync(request.key.Value, userId.Value)
            ? Ok()
            : Forbid();
    }

    /// <summary>GET VaultData/access/{entryId} — список UserId, кому открыт доступ (для поля "Доступ" в форме).</summary>
    [HttpGet("access/{entryId:int}")]
    public async Task<IActionResult> GetAccess(int entryId)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var userIds = await _vaultRepository.GetAccessUserIdsAsync(entryId, userId.Value);
        return Json(userIds);
    }

    /// <summary>POST VaultData/access — заменить список пользователей с доступом. Только для автора записи.</summary>
    [HttpPost("access")]
    public async Task<IActionResult> SetAccess([FromBody] SetAccessRequest request)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        return await _vaultRepository.SetAccessAsync(request.EntryId, userId.Value, request.UserIds ?? new List<int>())
            ? Ok()
            : Forbid();
    }

    private int? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }

    private static VaultRow ToRow(VaultEntry entry, int currentUserId) => new()
    {
        Id = entry.Id,
        Title = entry.Title,
        Login = entry.Login,
        Password = entry.Password,
        Url = entry.Url,
        Notes = entry.Notes,
        Tags = entry.Tags,
        CreatedByUserId = entry.CreatedByUserId,
        IsOwner = entry.CreatedByUserId == currentUserId,
        CreatedDate = entry.CreatedDate,
        ModifiedDate = entry.ModifiedDate
    };
}
