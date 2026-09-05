using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;

namespace Crm.Entity.Services;

public class ClientRepository : IClientRepository
{
    public async Task<List<Client>> GetAllAsync()
    {
        await using var db = new CrmContext();
        return await db.Clients.AsNoTracking().OrderBy(client => client.Name).ToListAsync();
    }

    public async Task<Client?> GetAsync(int id)
    {
        await using var db = new CrmContext();
        return await db.Clients.FirstOrDefaultAsync(client => client.ClientId == id);
    }

    public async Task<Client> AddAsync(Client client)
    {
        await using var db = new CrmContext();
        db.Clients.Add(client);
        await db.SaveChangesAsync();
        return client;
    }

    public async Task<bool> UpdateAsync(Client client)
    {
        await using var db = new CrmContext();
        var existing = await db.Clients.FirstOrDefaultAsync(item => item.ClientId == client.ClientId);
        if (existing == null)
            return false;

        existing.Name = client.Name;
        existing.ContactPerson = client.ContactPerson;
        existing.Phone = client.Phone;
        existing.Email = client.Email;
        existing.Address = client.Address;
        existing.TaxNumber = client.TaxNumber;
        existing.Notes = client.Notes;
        existing.IsActive = client.IsActive;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var db = new CrmContext();
        var client = await db.Clients.FirstOrDefaultAsync(item => item.ClientId == id);
        if (client == null)
            return false;

        db.Clients.Remove(client);
        await db.SaveChangesAsync();
        return true;
    }
}
