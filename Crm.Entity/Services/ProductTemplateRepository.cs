using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;

namespace Crm.Entity.Services;

public class ProductTemplateRepository : IProductTemplateRepository
{
    public async Task<List<ProductTemplate>> GetByUserAsync(int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return new List<ProductTemplate>();

        return await db.ProductTemplates
            .AsNoTracking()
            .Where(template => template.CompanyId == companyId.Value)
            .OrderBy(template => template.Name)
            .ToListAsync();
    }

    public async Task<ProductTemplate?> GetWithComponentsAsync(int id, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return null;

        return await db.ProductTemplates
            .AsNoTracking()
            .Include(template => template.ProductTemplateComponents)
                .ThenInclude(ptc => ptc.Component)
            .FirstOrDefaultAsync(template => template.ProductTemplateId == id && template.CompanyId == companyId.Value);
    }

    public async Task<ProductTemplate?> GetAsync(int id, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return null;

        return await db.ProductTemplates
            .FirstOrDefaultAsync(template => template.ProductTemplateId == id && template.CompanyId == companyId.Value);
    }

    public async Task<ProductTemplate> AddAsync(ProductTemplate template, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            throw new InvalidOperationException("У пользователя не выбрана текущая компания.");

        template.CompanyId = companyId.Value;
        db.ProductTemplates.Add(template);
        await db.SaveChangesAsync();
        return template;
    }

    public async Task<bool> UpdateAsync(ProductTemplate template, int userId)
    {
        var existing = await GetAsync(template.ProductTemplateId, userId);
        if (existing == null)
            return false;

        await using var db = new CrmContext();
        existing.Name = template.Name;
        existing.Category = template.Category;
        existing.Unit = template.Unit;
        existing.FormulaExpression = template.FormulaExpression;
        existing.DefaultMarginPercent = template.DefaultMarginPercent;
        existing.IsActive = template.IsActive;
        db.ProductTemplates.Update(existing);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var existing = await GetAsync(id, userId);
        if (existing == null)
            return false;

        await using var db = new CrmContext();
        db.ProductTemplates.Remove(existing);
        await db.SaveChangesAsync();
        return true;
    }

    private static async Task<int?> GetCompanyIdAsync(CrmContext db, int userId)
        => await db.Users
            .Where(user => user.Id == userId)
            .Select(user => user.CurrentCompanyId)
            .FirstOrDefaultAsync();
}
