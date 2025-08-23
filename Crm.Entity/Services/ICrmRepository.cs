using Crm.Core.Infrastructure;
using Crm.Entity.ModelsCrm;

namespace Crm.Entity.Services
{
    public interface ICrmRepository
    {
        TransactionResult<User> GetUser(string email);
        IEnumerable<User> GetUsers();
    }
}