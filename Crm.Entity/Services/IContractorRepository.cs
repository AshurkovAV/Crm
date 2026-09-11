using Crm.Entity.ModelsCrm;

namespace Crm.Entity.Services;

/// <summary>
/// CRUD подрядчиков (Модуль В ТЗ), company-scoped через CurrentCompanyId пользователя.
/// </summary>
public interface IContractorRepository
{
    Task<List<Contractor>> GetByUserAsync(int userId);

    Task<Contractor?> GetAsync(int id, int userId);

    Task<Contractor> AddAsync(Contractor contractor, int userId);

    Task<bool> UpdateAsync(Contractor contractor, int userId);

    Task<bool> DeleteAsync(int id, int userId);
}
