using Crm.Entity.ModelsCrm;

namespace Crm.Entity.Services;

public interface IVaultRepository
{
    Task<List<VaultEntry>> GetAccessibleAsync(int userId);
    Task<VaultEntry?> GetAsync(int id, int userId);
    Task<VaultEntry?> AddAsync(VaultEntry entry, int userId);
    Task<bool> UpdateAsync(VaultEntry entry, int userId);
    Task<bool> DeleteAsync(int id, int userId);
    Task<List<int>> GetAccessUserIdsAsync(int entryId, int userId);
    Task<bool> SetAccessAsync(int entryId, int userId, List<int> userIds);
}
