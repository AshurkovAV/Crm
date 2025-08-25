using Crm.Core.Infrastructure;
using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace Crm.Entity.Services
{
    public class UserRepository : IUserRepository
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

        public async Task AddAsync(User user)
        {
            using (var db = new CrmContext())
            {
                // Проверяем, что пользователь еще не добавлен в контекст
                var existingEntry = db.ChangeTracker.Entries<User>()
                    .FirstOrDefault(e => e.Entity.Id == user.Id);

                if (existingEntry == null)
                {
                    await db.Users.AddAsync(user);
                }
                else
                {
                    // Если уже отслеживается, просто обновляем состояние
                    existingEntry.State = EntityState.Added;
                }
                db.SaveChanges();
            }
               
        }
    }
}
