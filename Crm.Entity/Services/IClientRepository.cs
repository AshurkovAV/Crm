using Crm.Entity.ModelsCrm;

namespace Crm.Entity.Services;

public interface IClientRepository
{
    Task<List<Client>> GetAllAsync();
    Task<Client?> GetAsync(int id);
    Task<Client> AddAsync(Client client);
    Task<bool> UpdateAsync(Client client);
    Task<bool> DeleteAsync(int id);
}
