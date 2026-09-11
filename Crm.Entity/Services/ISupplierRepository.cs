using Crm.Entity.ModelsCrm;

namespace Crm.Entity.Services;

public interface ISupplierRepository
{
    Task<List<Supplier>> GetByUserAsync(int userId);
    Task<Supplier?> GetAsync(int id, int userId);
    Task<Supplier> AddAsync(Supplier supplier, int userId);
    Task<bool> UpdateAsync(Supplier supplier, int userId);
    Task<bool> DeleteAsync(int id, int userId);
}
