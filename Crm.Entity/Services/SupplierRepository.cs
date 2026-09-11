using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;

namespace Crm.Entity.Services;

public class SupplierRepository : ISupplierRepository
{
    public async Task<List<Supplier>> GetByUserAsync(int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return new List<Supplier>();

        return await db.Suppliers
            .AsNoTracking()
            .Where(supplier => supplier.CompanyId == companyId.Value)
            .OrderBy(supplier => supplier.Name)
            .ToListAsync();
    }

    public async Task<Supplier?> GetAsync(int id, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return null;

        return await db.Suppliers
            .FirstOrDefaultAsync(supplier => supplier.SupplierId == id && supplier.CompanyId == companyId.Value);
    }

    public async Task<Supplier> AddAsync(Supplier supplier, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            throw new InvalidOperationException("У пользователя не выбрана текущая компания.");

        supplier.CompanyId = companyId.Value;
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();
        return supplier;
    }

    public async Task<bool> UpdateAsync(Supplier supplier, int userId)
    {
        var existing = await GetAsync(supplier.SupplierId, userId);
        if (existing == null)
            return false;

        await using var db = new CrmContext();
        existing.Name = supplier.Name;
        existing.ContactPerson = supplier.ContactPerson;
        existing.Phone = supplier.Phone;
        existing.Email = supplier.Email;
        existing.Notes = supplier.Notes;
        existing.IsActive = supplier.IsActive;
        db.Suppliers.Update(existing);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var existing = await GetAsync(id, userId);
        if (existing == null)
            return false;

        await using var db = new CrmContext();
        db.Suppliers.Remove(existing);
        await db.SaveChangesAsync();
        return true;
    }

    private static async Task<int?> GetCompanyIdAsync(CrmContext db, int userId)
        => await db.Users
            .Where(user => user.Id == userId)
            .Select(user => user.CurrentCompanyId)
            .FirstOrDefaultAsync();
}
