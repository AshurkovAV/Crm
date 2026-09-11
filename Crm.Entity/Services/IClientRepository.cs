using Crm.Entity.ModelsCrm;

namespace Crm.Entity.Services;

public interface IClientRepository
{
    Task<List<Client>> GetByUserAsync(int userId);
    Task<Client?> GetAsync(int id, int userId);
    Task<Client> AddAsync(Client client, int userId);
    Task<bool> UpdateAsync(Client client, int userId);
    Task<bool> DeleteAsync(int id, int userId);
}
