using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;

namespace Crm.Entity.Services;

public class ComponentRepository : IComponentRepository
{
    public async Task<List<Component>> GetByUserAsync(int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return new List<Component>();

        return await db.Components
            .AsNoTracking()
            .Include(component => component.Supplier)
            .Where(component => component.CompanyId == companyId.Value)
            .OrderBy(component => component.Name)
            .ToListAsync();
    }

    public async Task<Component?> GetAsync(int id, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return null;

        return await db.Components
            .FirstOrDefaultAsync(component => component.ComponentId == id && component.CompanyId == companyId.Value);
    }

    public async Task<List<Component>> GetByIdsAsync(IEnumerable<int> ids, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return new List<Component>();

        var idList = ids.Distinct().ToList();
        return await db.Components
            .AsNoTracking()
            .Where(component => component.CompanyId == companyId.Value && idList.Contains(component.ComponentId))
            .ToListAsync();
    }

    public async Task<Component> AddAsync(Component component, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            throw new InvalidOperationException("У пользователя не выбрана текущая компания.");

        component.CompanyId = companyId.Value;
        db.Components.Add(component);
        await db.SaveChangesAsync();
        return component;
    }

    public async Task<bool> UpdateAsync(Component component, int userId)
    {
        var existing = await GetAsync(component.ComponentId, userId);
        if (existing == null)
            return false;

        await using var db = new CrmContext();
        existing.Name = component.Name;
        existing.Unit = component.Unit;
        existing.CostPrice = component.CostPrice;
        existing.StockQuantity = component.StockQuantity;
        existing.ReorderLevel = component.ReorderLevel;
        existing.SupplierId = component.SupplierId;
        existing.IsActive = component.IsActive;
        db.Components.Update(existing);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var existing = await GetAsync(id, userId);
        if (existing == null)
            return false;

        await using var db = new CrmContext();
        db.Components.Remove(existing);
        await db.SaveChangesAsync();
        return true;
    }

    private static async Task<int?> GetCompanyIdAsync(CrmContext db, int userId)
        => await db.Users
            .Where(user => user.Id == userId)
            .Select(user => user.CurrentCompanyId)
            .FirstOrDefaultAsync();
}
