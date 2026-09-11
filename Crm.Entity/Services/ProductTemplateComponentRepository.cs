using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;

namespace Crm.Entity.Services;

public class ProductTemplateComponentRepository : IProductTemplateComponentRepository
{
    public async Task<List<ProductTemplateComponent>> GetByTemplateAsync(int productTemplateId, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return new List<ProductTemplateComponent>();

        return await db.ProductTemplateComponents
            .AsNoTracking()
            .Include(item => item.Component)
            .Where(item => item.ProductTemplateId == productTemplateId
                           && item.ProductTemplate.CompanyId == companyId.Value)
            .ToListAsync();
    }

    public async Task<ProductTemplateComponent?> GetAsync(int id, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return null;

        return await db.ProductTemplateComponents
            .Include(item => item.Component)
            .FirstOrDefaultAsync(item => item.Id == id && item.ProductTemplate.CompanyId == companyId.Value);
    }

    public async Task<ProductTemplateComponent?> AddAsync(ProductTemplateComponent item, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            throw new InvalidOperationException("У пользователя не выбрана текущая компания.");

        var templateOwnedByCompany = await db.ProductTemplates
            .AnyAsync(t => t.ProductTemplateId == item.ProductTemplateId && t.CompanyId == companyId.Value);
        if (!templateOwnedByCompany)
            return null;

        db.ProductTemplateComponents.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    public async Task<bool> UpdateAsync(ProductTemplateComponent item, int userId)
    {
        var existing = await GetAsync(item.Id, userId);
        if (existing == null)
            return false;

        await using var db = new CrmContext();
        existing.ComponentId = item.ComponentId;
        existing.QuantityFormula = item.QuantityFormula;
        existing.Notes = item.Notes;
        db.ProductTemplateComponents.Update(existing);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var existing = await GetAsync(id, userId);
        if (existing == null)
            return false;

        await using var db = new CrmContext();
        db.ProductTemplateComponents.Remove(existing);
        await db.SaveChangesAsync();
        return true;
    }

    private static async Task<int?> GetCompanyIdAsync(CrmContext db, int userId)
        => await db.Users
            .Where(user => user.Id == userId)
            .Select(user => user.CurrentCompanyId)
            .FirstOrDefaultAsync();
}
