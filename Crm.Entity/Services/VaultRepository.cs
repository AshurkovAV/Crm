using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;

namespace Crm.Entity.Services;

/// <summary>
/// Хранилище паролей. ВАЖНО: Password не шифруется — сознательное решение заказчика.
/// Доступ к записи ограничен только на уровне приложения: её видит автор
/// (CreatedByUserId) и пользователи, явно добавленные в VaultEntryAccess.
/// Редактировать и удалять запись, а также менять список доступа — может только автор.
/// </summary>
public class VaultRepository : IVaultRepository
{
    public async Task<List<VaultEntry>> GetAccessibleAsync(int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return new List<VaultEntry>();

        return await db.VaultEntries
            .AsNoTracking()
            .Where(e => e.CompanyId == companyId.Value)
            .Where(e => e.CreatedByUserId == userId || e.AccessList.Any(a => a.UserId == userId))
            .OrderByDescending(e => e.ModifiedDate)
            .ToListAsync();
    }

    public async Task<VaultEntry?> GetAsync(int id, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return null;

        return await db.VaultEntries
            .Where(e => e.CompanyId == companyId.Value)
            .Where(e => e.CreatedByUserId == userId || e.AccessList.Any(a => a.UserId == userId))
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<VaultEntry?> AddAsync(VaultEntry entry, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return null;

        entry.Id = 0;
        entry.CompanyId = companyId.Value;
        entry.CreatedByUserId = userId;
        entry.CreatedDate = DateTime.UtcNow;
        entry.ModifiedDate = DateTime.UtcNow;

        db.VaultEntries.Add(entry);
        await db.SaveChangesAsync();
        return entry;
    }

    public async Task<bool> UpdateAsync(VaultEntry entry, int userId)
    {
        await using var db = new CrmContext();
        var existing = await db.VaultEntries.FirstOrDefaultAsync(e => e.Id == entry.Id);
        if (existing == null || existing.CreatedByUserId != userId)
            return false;

        existing.Title = entry.Title;
        existing.Login = entry.Login;
        existing.Password = entry.Password;
        existing.Url = entry.Url;
        existing.Notes = entry.Notes;
        existing.Tags = entry.Tags;
        existing.ModifiedDate = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        await using var db = new CrmContext();
        var existing = await db.VaultEntries.FirstOrDefaultAsync(e => e.Id == id);
        if (existing == null || existing.CreatedByUserId != userId)
            return false;

        db.VaultEntries.Remove(existing);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<int>> GetAccessUserIdsAsync(int entryId, int userId)
    {
        var entry = await GetAsync(entryId, userId);
        if (entry == null)
            return new List<int>();

        await using var db = new CrmContext();
        return await db.VaultEntryAccesses
            .Where(a => a.VaultEntryId == entryId)
            .Select(a => a.UserId)
            .ToListAsync();
    }

    public async Task<bool> SetAccessAsync(int entryId, int userId, List<int> userIds)
    {
        await using var db = new CrmContext();
        var existing = await db.VaultEntries.FirstOrDefaultAsync(e => e.Id == entryId);
        if (existing == null || existing.CreatedByUserId != userId)
            return false;

        var currentAccess = await db.VaultEntryAccesses
            .Where(a => a.VaultEntryId == entryId)
            .ToListAsync();
        db.VaultEntryAccesses.RemoveRange(currentAccess);

        var distinctUserIds = userIds.Distinct().Where(id => id != userId);
        foreach (var grantedUserId in distinctUserIds)
        {
            db.VaultEntryAccesses.Add(new VaultEntryAccess
            {
                VaultEntryId = entryId,
                UserId = grantedUserId,
                GrantedDate = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        return true;
    }

    private static async Task<int?> GetCompanyIdAsync(CrmContext db, int userId)
        => await db.Users
            .Where(user => user.Id == userId)
            .Select(user => user.CurrentCompanyId)
            .FirstOrDefaultAsync();
}
