using Crm.Entity.ModelsCrm;

namespace Crm.Entity.Services;

public interface IDealRepository
{
    Task<List<Deal>> GetByUserAsync(int userId);

    Task<Deal?> GetAsync(int id, int userId);

    Task<Deal> AddAsync(Deal deal);

    Task<bool> UpdateAsync(Deal deal, int userId);

    Task<bool> DeleteAsync(int id, int userId);
}
