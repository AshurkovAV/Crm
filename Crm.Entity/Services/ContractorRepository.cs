using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;

namespace Crm.Entity.Services;

public class ContractorRepository : IContractorRepository
{
    public async Task<List<Contractor>> GetByUserAsync(int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return new List<Contractor>();

        return await db.Contractors
            .AsNoTracking()
            .Where(contractor => contractor.CompanyId == companyId.Value)
            .OrderBy(contractor => contractor.Name)
            .ToListAsync();
    }

    public async Task<Contractor?> GetAsync(int id, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return null;

        return await db.Contractors
            .FirstOrDefaultAsync(contractor => contractor.ContractorId == id && contractor.CompanyId == companyId.Value);
    }

    public async Task<Contractor> AddAsync(Contractor contractor, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            throw new InvalidOperationException("У пользователя не выбрана текущая компания.");

        contractor.CompanyId = companyId.Value;
        db.Contractors.Add(contractor);
        await db.SaveChangesAsync();
        return contractor;
    }

    public async Task<bool> UpdateAsync(Contractor contractor, int userId)
    {
        var existing = await GetAsync(contractor.ContractorId, userId);
        if (existing == null)
            return false;

        await using var db = new CrmContext();
        existing.Name = contractor.Name;
        existing.Phone = contractor.Phone;
        existing.Email = contractor.Email;
        existing.Notes = contractor.Notes;
        existing.IsActive = contractor.IsActive;
        db.Contractors.Update(existing);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var existing = await GetAsync(id, userId);
        if (existing == null)
            return false;

        await using var db = new CrmContext();
        db.Contractors.Remove(existing);
        await db.SaveChangesAsync();
        return true;
    }

    private static async Task<int?> GetCompanyIdAsync(CrmContext db, int userId)
        => await db.Users
            .Where(user => user.Id == userId)
            .Select(user => user.CurrentCompanyId)
            .FirstOrDefaultAsync();
}
