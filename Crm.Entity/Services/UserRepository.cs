using Crm.Core.Infrastructure;
using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
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

        public TransactionResult<User> GetUserById(int id)
        {
            var result = new TransactionResult<User>();
            try
            {
                using (var db = new CrmContext())
                {
                    var user = db.Users.Where(x => x.Id == id && x.IsActive == true).FirstOrDefault();
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

        public async Task<bool> VerifyPasswordAsync(string email, string password)
        {
            try
            {
                using (var db = new CrmContext())
                {
                    var user = await db.Users
                    .FirstOrDefaultAsync(u => u.DefaultEmail == email && u.IsActive);

                    if (user == null || string.IsNullOrEmpty(user.PasswordHash) || string.IsNullOrEmpty(user.PasswordSalt))
                    {
                        return false;
                    }

                    // Хешируем введенный пароль с солью пользователя
                    var inputHash = HashPassword(password, user.PasswordSalt);

                    // Сравниваем хеши
                    return inputHash == user.PasswordHash;
                }
                    
            }
            catch (Exception ex)
            {               
                return false;
            }
        }

        public async Task<bool> SaveRememberTokenAsync(string email, string rememberToken, string deviceId)
        {
            try
            {
                using (var db = new CrmContext())
                {
                    // Удаляем старый токен для этого устройства (если есть)
                    var existing = await db.RememberedDevices
                        .FirstOrDefaultAsync(rd => rd.Email == email && rd.DeviceId == deviceId);

                    if (existing != null)
                    {
                        db.RememberedDevices.Remove(existing);
                    }

                    var user = await db.Users
                        .FirstOrDefaultAsync(rd => rd.DefaultEmail == email && rd.IsActive == true);
                    if (user == null)
                    {
                        throw new Exception("Пользователь не найден либо не активен");
                    }
                    // Сохраняем новый токен
                    var rememberedDevice = new RememberedDevice
                    {
                        Email = email,
                        RememberToken = rememberToken,
                        DeviceId = deviceId,
                        UserId = user.Id,
                        Expiration = DateTime.UtcNow.AddDays(30),
                        CreatedAt = DateTime.UtcNow
                    };

                    db.RememberedDevices.Add(rememberedDevice);
                    await db.SaveChangesAsync();

                    return true;
                }

            }
            catch (Exception ex)
            {                
                return false;
            }
        }

        public async Task<bool> SetPasswordAsync(string email, string password)
        {
            try
            {
                using (var db = new CrmContext())
                {
                    // Находим пользователя
                    var user = await db.Users
                    .FirstOrDefaultAsync(u => u.DefaultEmail == email && u.IsActive);

                    if (user == null)
                    {                        
                        return false;
                    }

                    // Генерируем соль и хеш пароля
                    var salt = GenerateSalt();
                    var passwordHash = HashPassword(password, salt);

                    // Обновляем пароль
                    user.PasswordHash = passwordHash;
                    user.PasswordSalt = salt;                    
                    user.ModifiedDate = DateTime.UtcNow;
                    user.IsValidation = true;
                    db.Users.Update(user);

                    await db.SaveChangesAsync();
                    
                    return true;
                }
                    
            }
            catch (Exception ex)
            {                
                return false;
            }
        }

        // Вспомогательные методы для работы с паролями
        private string GenerateSalt()
        {
            var saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        private string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var saltedPassword = password + salt;
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                return Convert.ToBase64String(hashedBytes);
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
