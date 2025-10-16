using Crm.Core.Infrastructure;
using Crm.Entity.ModelsCrm;
using System.Threading.Tasks;

namespace Crm.Entity.Services
{
    public interface IUserRepository
    {
        Task AddAsync(User user);
        Task AddOrUpdateAsync(User user);
        TransactionResult<User> GetUser(string email);
        IEnumerable<User> GetUsers();
        UserToken InsertUserToken(UserToken user);
        Task<bool> SetPasswordAsync(string email, string password);
        Task<bool> VerifyPasswordAsync(string email, string password);
        Task<bool> SaveRememberTokenAsync(string email, string rememberToken, string deviceId);
    }
}