using Crm.Entity.ModelsCrm;
using Crm.Core.Infrastructure;

namespace Crm.Entity.Services
{
    public class CrmRepository : ICrmRepository
    {

        public IEnumerable<User> GetUsers()
        {
            var result = new List<User>();
            using (var db = new CrmContext())
            {
                result = db.Users.Where(x => x.IsActive == true).ToList();
            }
            return result;
        }

        public TransactionResult<User> GetUser(string email)
        {
            var result = new TransactionResult<User>();
            try
            {
                using (var db = new CrmContext())
                {
                    var user = db.Users.Where(x => x.DefaultEmail == email && x.IsActive == true).FirstOrDefault();
                    if (user == null)
                    {
                        throw new Exception("Пользователь не найден, либо не активен");
                    }
                    result.Data = user;
                }
            }
            catch (Exception ex)
            {
                result.AddError(ex);
            }

            return result;
        }
    }
}
