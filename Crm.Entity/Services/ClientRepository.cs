using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;

namespace Crm.Entity.Services;

public class ClientRepository : IClientRepository
{
    public async Task<List<Client>> GetByUserAsync(int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return new List<Client>();

        return await db.Clients
            .AsNoTracking()
            .Where(client => client.CompanyId == companyId.Value)
            .OrderBy(client => client.Name)
            .ToListAsync();
    }

    public async Task<Client?> GetAsync(int id, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return null;

        return await db.Clients
            .FirstOrDefaultAsync(client => client.ClientId == id && client.CompanyId == companyId.Value);
    }

    public async Task<Client> AddAsync(Client client, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            throw new InvalidOperationException("У пользователя не выбрана текущая компания.");

        client.CompanyId = companyId.Value;
        db.Clients.Add(client);
        await db.SaveChangesAsync();
        return client;
    }

    public async Task<bool> UpdateAsync(Client client, int userId)
    {
        var existing = await GetAsync(client.ClientId, userId);
        if (existing == null)
            return false;

        await using var db = new CrmContext();
        existing.Name = client.Name;
        existing.ContactPerson = client.ContactPerson;
        existing.Phone = client.Phone;
        existing.Email = client.Email;
        existing.Address = client.Address;
        existing.TaxNumber = client.TaxNumber;
        existing.Notes = client.Notes;
        existing.IsActive = client.IsActive;
        db.Clients.Update(existing);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var existing = await GetAsync(id, userId);
        if (existing == null)
            return false;

        await using var db = new CrmContext();
        db.Clients.Remove(existing);
        await db.SaveChangesAsync();
        return true;
    }

    private static async Task<int?> GetCompanyIdAsync(CrmContext db, int userId)
        => await db.Users
            .Where(user => user.Id == userId)
            .Select(user => user.CurrentCompanyId)
            .FirstOrDefaultAsync();
}
