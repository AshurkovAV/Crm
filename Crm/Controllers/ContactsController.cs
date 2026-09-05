using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

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
        var clients = await _clientRepository.GetAllAsync();
        return Json(DataSourceLoader.Load(clients, loadOptions));
    }

    [HttpGet]
    public async Task<IActionResult> Lookup(DataSourceLoadOptions loadOptions)
    {
        var clients = (await _clientRepository.GetAllAsync())
            .Where(client => client.IsActive != false)
            .Select(client => new { client.ClientId, client.Name });
        return Json(DataSourceLoader.Load(clients, loadOptions));
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromForm] ClientValues form)
    {
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
        await _clientRepository.AddAsync(client);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut]
    public async Task<IActionResult> Put(int key, [FromForm] ClientValues form)
    {
        var client = await _clientRepository.GetAsync(key);
        if (client == null)
            return NotFound();

        JsonConvert.PopulateObject(form.Values ?? "{}", client);
        client.ClientId = key;
        client.Name = string.IsNullOrWhiteSpace(client.Name) ? "Новый клиент" : client.Name.Trim();
        return await _clientRepository.UpdateAsync(client) ? Ok() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => await _clientRepository.DeleteAsync(id) ? Ok() : NotFound();

    public sealed class ClientValues
    {
        public string? Key { get; set; }
        public string? Values { get; set; }
    }
}
