using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Claims;

namespace Crm.Controllers;

[Authorize]
public class ContactsController : Controller
{
    private readonly IClientRepository _clientRepository;

    public ContactsController(IClientRepository clientRepository)
    {
        _clientRepository = clientRepository;
    }

    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
    {
        var userId = GetUserId();
        var clients = userId.HasValue ? await _clientRepository.GetByUserAsync(userId.Value) : new List<Client>();
        return Json(DataSourceLoader.Load(clients, loadOptions));
    }

    [HttpGet]
    public async Task<IActionResult> Lookup(DataSourceLoadOptions loadOptions)
    {
        var userId = GetUserId();
        var clients = (userId.HasValue ? await _clientRepository.GetByUserAsync(userId.Value) : new List<Client>())
            .Where(client => client.IsActive != false)
            .Select(client => new { client.ClientId, client.Name });
        return Json(DataSourceLoader.Load(clients, loadOptions));
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromForm] ClientValues form)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var client = new Client
        {
            Name = "Новый клиент",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        JsonConvert.PopulateObject(form.Values ?? "{}", client);
        client.ClientId = 0;
        client.CreatedDate ??= DateTime.UtcNow;
        client.Name = string.IsNullOrWhiteSpace(client.Name) ? "Новый клиент" : client.Name.Trim();
        await _clientRepository.AddAsync(client, userId.Value);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut]
    public async Task<IActionResult> Put(int key, [FromForm] ClientValues form)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var client = await _clientRepository.GetAsync(key, userId.Value);
        if (client == null)
            return NotFound();

        JsonConvert.PopulateObject(form.Values ?? "{}", client);
        client.ClientId = key;
        client.Name = string.IsNullOrWhiteSpace(client.Name) ? "Новый клиент" : client.Name.Trim();
        return await _clientRepository.UpdateAsync(client, userId.Value) ? Ok() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        return await _clientRepository.DeleteAsync(id, userId.Value) ? Ok() : NotFound();
    }

    private int? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }

    public sealed class ClientValues
    {
        public string? Key { get; set; }
        public string? Values { get; set; }
    }
}
