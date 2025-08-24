using Crm.Core.Infrastructure;
using Crm.Entity.ModelsCrm;

namespace Crm.Entity.Services
{
    public interface IUserRepository
    {
        Task AddAsync(Entities.User user);
        TransactionResult<User> GetUser(string email);
        IEnumerable<User> GetUsers();
    }
}