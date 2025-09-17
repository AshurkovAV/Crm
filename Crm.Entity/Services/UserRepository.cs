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

        public async Task AddOrUpdateAsync(User user)
        {
            using (var db = new CrmContext())
            {
                // Проверяем существование пользователя в базе данных
                var existingUser = await db.Users
                    .AsNoTracking() // Чтобы не отслеживать сущность
                    .FirstOrDefaultAsync(u => u.Id == user.Id);

                if (existingUser == null)
                {
                    // Пользователя нет в базе - добавляем
                    await db.Users.AddAsync(user);
                }
                else
                {
                    // Пользователь существует - обновляем
                    db.Users.Update(user);

                    // Альтернативный вариант с более контролируемым обновлением:
                    // db.Entry(user).State = EntityState.Modified;
                }

                await db.SaveChangesAsync();
            }
        }
        public async Task AddAsync(User user)
        {
            using (var db = new CrmContext())
            {
                var data = db.Add(user);                
                await db.SaveChangesAsync();
            }
        }

        public UserToken InsertUserToken(UserToken user)
        {
            using (var db = new CrmContext())
            {
                // Проверяем, что пользователь еще не добавлен в контекст
                var existingEntry = db.ChangeTracker.Entries<UserToken>()
                    .FirstOrDefault(e => e.Entity.Id == user.Id);

                if (existingEntry == null)
                {
                    db.UserTokens.AddAsync(user);
                }
                else
                {
                    // Если уже отслеживается, просто обновляем состояние
                    existingEntry.State = EntityState.Added;
                }
                db.SaveChanges();
            }
            return user;
        }
    }
}
