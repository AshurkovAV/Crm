using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;

namespace Crm.Entity.Services;

public class DealRepository : IDealRepository
{
    public async Task<List<Deal>> GetByUserAsync(int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return new List<Deal>();

        return await db.Deals
            .AsNoTracking()
            .Include(deal => deal.Client)
            .Where(deal => deal.CompanyId == companyId.Value)
            .OrderByDescending(deal => deal.ModifiedDate)
            .ToListAsync();
    }

    public async Task<Deal?> GetAsync(int id, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return null;

        return await db.Deals
            .Include(deal => deal.Client)
            .FirstOrDefaultAsync(deal => deal.Id == id && deal.CompanyId == companyId.Value);
    }

    public async Task<Deal> AddAsync(Deal deal)
    {
        await using var db = new CrmContext();
        db.Deals.Add(deal);
        await db.SaveChangesAsync();
        return deal;
    }

    public async Task<bool> UpdateAsync(Deal deal, int userId)
    {
        var existing = await GetAsync(deal.Id, userId);
        if (existing == null)
            return false;

        existing.Title = deal.Title;
        existing.ClientId = deal.ClientId;
        existing.ClientName = deal.ClientName;
        existing.Amount = deal.Amount;
        existing.Status = deal.Status;
        existing.ExpectedCloseDate = deal.ExpectedCloseDate;
        existing.Description = deal.Description;
        existing.ModifiedDate = DateTime.UtcNow;

        await using var db = new CrmContext();
        db.Deals.Update(existing);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var existing = await GetAsync(id, userId);
        if (existing == null)
            return false;

        await using var db = new CrmContext();
        db.Deals.Remove(existing);
        await db.SaveChangesAsync();
        return true;
    }

    private static async Task<int?> GetCompanyIdAsync(CrmContext db, int userId)
        => await db.Users
            .Where(user => user.Id == userId)
            .Select(user => user.CurrentCompanyId)
            .FirstOrDefaultAsync();
}
