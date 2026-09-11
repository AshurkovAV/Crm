using Crm.Entity.ModelsCrm;

namespace Crm.Entity.Services;

public interface IComponentRepository
{
    Task<List<Component>> GetByUserAsync(int userId);
    Task<Component?> GetAsync(int id, int userId);
    Task<List<Component>> GetByIdsAsync(IEnumerable<int> ids, int userId);
    Task<Component> AddAsync(Component component, int userId);
    Task<bool> UpdateAsync(Component component, int userId);
    Task<bool> DeleteAsync(int id, int userId);
}
