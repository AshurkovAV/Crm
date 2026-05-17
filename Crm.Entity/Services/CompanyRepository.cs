using Crm.Core.Infrastructure;
using Crm.Entity.DTO;
using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;


namespace Crm.Entity.Services
{
    public class CompanyRepository : ICompanyRepository
    {
        /// <summary>
        /// Получить всех пользователей компании с их ролями
        /// </summary>
        public async Task<List<CompanyUserDto>> GetCompanyUsersAsync(int companyId, int currentUserId)
        {
            using (var db = new CrmContext())
            {

                // Проверяем, существует ли компания
                var company = await db.Companies
                .FirstOrDefaultAsync(c => c.Id == companyId && c.IsActive == true);

                if (company == null)
                    return new List<CompanyUserDto>();

                // Получаем всех пользователей компании
                var companyUsers = await db.CompanyUsers
                    .Where(cu => cu.CompanyId == companyId && cu.IsActive == true)
                    .Include(cu => cu.User)
                    .Select(cu => new CompanyUserDto
                    {
                        UserId = cu.User.Id,
                        Login = cu.User.Login,
                        DisplayName = cu.User.DisplayName,
                        FirstName = cu.User.FirstName,
                        LastName = cu.User.LastName,
                        Email = cu.User.DefaultEmail,
                        Phone = cu.User.DefaultPhone,
                        Role = cu.Role,
                        Position = cu.Position,
                        JoinedDate = cu.JoinedDate,
                        IsActive = cu.IsActive,
                        IsOwner = cu.User.Id == company.OwnerId,
                        AvatarUrl = cu.User.DefaultAvatarId != null && cu.User.IsAvatarEmpty != "true"
                            ? $"/api/avatars/{cu.User.DefaultAvatarId}"
                            : null
                    })
                    .OrderByDescending(u => u.IsOwner)  // сначала владелец
                    .ThenBy(u => u.Role)                // потом по роли
                    .ThenBy(u => u.DisplayName ?? u.Login)
                    .ToListAsync();

                return companyUsers;
            }
        }

        /// <summary>
        /// Получить всех пользователей компании по ID текущего пользователя
        /// </summary>
        public async Task<List<CompanyUserDto>> GetCompanyUsersByUserIdAsync(int userId)
        {
            using (var db = new CrmContext())
            {
                // Получаем текущего пользователя с его CurrentCompanyId
                var currentUser = await db.Users
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.Id, u.CurrentCompanyId })
                    .FirstOrDefaultAsync();

                if (currentUser == null)
                {
                    return new List<CompanyUserDto>();
                }

                // Если у пользователя нет CurrentCompanyId - возвращаем только его самого
                if (!currentUser.CurrentCompanyId.HasValue)
                {
                    var userOnly = await db.Users
                        .Where(u => u.Id == userId)
                        .Select(u => new CompanyUserDto
                        {
                            UserId = u.Id,
                            Login = u.Login,
                            DisplayName = u.DisplayName,
                            FirstName = u.FirstName,
                            LastName = u.LastName,
                            Email = u.DefaultEmail,
                            Phone = u.DefaultPhone,
                            Role = "Владелец аккаунта",
                            Position = null,
                            IsActive = true,
                            IsOwner = true,
                            IsCurrentUser = true,
                            AvatarUrl = u.DefaultAvatarId != null && u.IsAvatarEmpty != "true"
                                ? $"/api/avatars/{u.DefaultAvatarId}"
                                : null
                        })
                        .ToListAsync();

                    return userOnly;
                }

                var companyId = currentUser.CurrentCompanyId.Value;

                // Проверяем, существует ли компания и активна ли она
                var company = await db.Companies
                    .Where(c => c.Id == companyId && c.IsActive == true)
                    .FirstOrDefaultAsync();

                if (company == null)
                {
                    return new List<CompanyUserDto>();
                }

                // Получаем всех активных пользователей компании
                var companyUsers = await db.CompanyUsers
                    .Where(cu => cu.CompanyId == companyId && cu.IsActive == true)
                    .Include(cu => cu.User)
                    .Select(cu => new CompanyUserDto
                    {
                        UserId = cu.User.Id,
                        Login = cu.User.Login,
                        DisplayName = cu.User.DisplayName,
                        FirstName = cu.User.FirstName,
                        LastName = cu.User.LastName,
                        Email = cu.User.DefaultEmail,
                        Phone = cu.User.DefaultPhone,
                        Role = cu.Role,
                        Position = cu.Position,
                        JoinedDate = cu.JoinedDate,
                        IsActive = cu.IsActive,
                        IsOwner = cu.User.Id == company.OwnerId,
                        IsCurrentUser = cu.User.Id == userId,
                        AvatarUrl = cu.User.DefaultAvatarId != null && cu.User.IsAvatarEmpty != "true"
                            ? $"/api/avatars/{cu.User.DefaultAvatarId}"
                            : null,
                        CompanyId = companyId,
                        CompanyName = company.Name
                    })
                    .OrderByDescending(u => u.IsOwner)      // Владелец первый
                    .ThenByDescending(u => u.IsCurrentUser) // Текущий пользователь второй
                    .ThenBy(u => u.Role)                    // По роли
                    .ThenBy(u => u.DisplayName ?? u.Login)  // По имени
                    .ToListAsync();

                return companyUsers;
            }
        }

        /// <summary>
        /// Получить пользователя со всеми его компаниями (активными)
        /// </summary>
        public ModelsCrm.User GetUserWithCompanies(int userId)
        {
            using (var _context = new CrmContext())
            {
                var user = _context.Users
                .Include(u => u.CompanyUsers) // Загружаем связи с компаниями
                .FirstOrDefault(u => u.Id == userId);

                if (user == null)
                    return null;

                // Загружаем детали компаний отдельно (опционально через Include)
                var companyIds = user.CompanyUsers.Where(cu => cu.IsActive == true).Select(cu => cu.CompanyId).ToList();

                if (companyIds.Any())
                {
                    // Принудительно загружаем компании, чтобы EF их подтянул
                    _context.Companies
                        .Where(c => companyIds.Contains(c.Id))
                        .Load();
                }

                return user;
            }
        }

        /// <summary>
        /// Получить список активных компаний пользователя
        /// </summary>
        public List<CompanyUser> GetUserActiveCompanies(int userId)
        {
            using (var _context = new CrmContext())
            {
                return _context.CompanyUsers
                .Include(cu => cu.Company)
                .Where(cu => cu.UserId == userId && cu.IsActive == true)
                .OrderByDescending(cu => cu.JoinedDate)
                .ToList();
            }
        }

        /// <summary>
        /// Переключить текущую компанию пользователя
        /// </summary>
        public bool UpdateUserCurrentCompany(int userId, int companyId)
        {
            using (var _context = new CrmContext())
            {
                // Проверяем, принадлежит ли компания пользователю и активна ли она
                var companyUser = _context.CompanyUsers
                .FirstOrDefault(cu => cu.UserId == userId
                    && cu.CompanyId == companyId
                    && cu.IsActive == true);

                if (companyUser == null)
                    return false;

                var user = _context.Users.Find(userId);
                if (user == null)
                    return false;

                user.CurrentCompanyId = companyId;
                user.ModifiedDate = DateTime.Now;

                _context.SaveChanges();
                return true;
            }
        }

        public async Task<List<CompanyUserDto>> GetCompanyUsersAsync(int companyId)
        {
            using (var db = new CrmContext())
            {

                // Проверяем, существует ли компания
                var company = await db.Companies
                .FirstOrDefaultAsync(c => c.Id == companyId && c.IsActive == true);

                if (company == null)
                    return new List<CompanyUserDto>();

                // Получаем всех пользователей компании
                var companyUsers = await db.CompanyUsers
                    .Where(cu => cu.CompanyId == companyId && cu.IsActive == true)
                    .Include(cu => cu.User)
                    .Select(cu => new CompanyUserDto
                    {
                        UserId = cu.User.Id,
                        Login = cu.User.Login,
                        DisplayName = cu.User.DisplayName,
                        FirstName = cu.User.FirstName,
                        LastName = cu.User.LastName,
                        Email = cu.User.DefaultEmail,
                        Phone = cu.User.DefaultPhone,
                        Role = cu.Role,
                        Position = cu.Position,
                        JoinedDate = cu.JoinedDate,
                        IsActive = cu.IsActive,
                        IsOwner = cu.User.Id == company.OwnerId,
                        AvatarUrl = cu.User.DefaultAvatarId != null && cu.User.IsAvatarEmpty != "true"
                            ? $"/api/avatars/{cu.User.DefaultAvatarId}"
                            : null
                    })
                    .OrderByDescending(u => u.IsOwner)  // сначала владелец
                    .ThenBy(u => u.Role)                // потом по роли
                    .ThenBy(u => u.DisplayName ?? u.Login)
                    .ToListAsync();

                return companyUsers;
            }
        }
        public async Task<bool> UserHasOwnCompanyAsync(int userId)
        {
            using (var db = new CrmContext())
            {
                return await db.Companies.AnyAsync(c => c.OwnerId == userId && c.IsActive == true);
            }
        }

        public async Task<TransactionResult<int>> GetCompanyIdToUserId(int userId)
        {
            var result = new TransactionResult<int>();
            try
            {
                using (var db = new CrmContext())
                {
                    var resultData = db.Companies.FirstOrDefault(c => c.OwnerId == userId && c.IsActive == true);
                    if (resultData == null)
                    {
                        throw new Exception("Нет активного пользователя");
                    }
                    result.Data = resultData.Id;
                }
            }
            catch(Exception ex)
            {
                result.AddError(ex.Message);
            }
            return result;
            
        }

        public async Task<TransactionResult<CompanyUser>> GetCompanyUserToUserId(int userId, int? conmanyId)
        {
            var result = new TransactionResult<CompanyUser>();
            try
            {
                using (var db = new CrmContext())
                {
                    CompanyUser resultData;
                    if (conmanyId == null)
                    {
                        resultData = db.CompanyUsers.FirstOrDefault(c => c.UserId == userId && c.IsActive == true);
                    }
                    else
                    {
                        resultData = db.CompanyUsers.FirstOrDefault(c => c.UserId == userId && c.CompanyId == conmanyId && c.IsActive == true);
                    }
                    if (resultData == null)
                    {
                        throw new Exception("Нет активного пользователя");
                    }
                    result.Data = resultData;
                }
            }
            catch (Exception ex)
            {
                result.AddError(ex.Message);
            }
            return result;

        }
        public async Task AddOrUpdateAsync(CompanyUser companyUser)
        {
            using (var db = new CrmContext())
            {
                // Проверяем существование пользователя в базе данных
                var existingUser = await db.CompanyUsers
                    .AsNoTracking() // Чтобы не отслеживать сущность
                    .FirstOrDefaultAsync(u => u.Id == companyUser.Id);

                if (existingUser == null)
                {
                    // Пользователя нет в базе - добавляем
                    await db.CompanyUsers.AddAsync(companyUser);
                }
                else
                {
                    // Пользователь существует - обновляем
                    db.CompanyUsers.Update(companyUser);

                    // Альтернативный вариант с более контролируемым обновлением:
                    // db.Entry(user).State = EntityState.Modified;
                }

                await db.SaveChangesAsync();
            }
        }

        public async Task<bool> UserHasAnyCompanyAsync(int userId)
        {
            using (var db = new CrmContext())
            {
                return await db.CompanyUsers.AnyAsync(cu => cu.UserId == userId && cu.IsActive == true);
            }
        }

        public async Task AddAsync(ModelsCrm.Company company)
        {
            using (var db = new CrmContext())
            {
                var data = db.Add(company);
                await db.SaveChangesAsync();
            }
        }

        public async Task<TransactionResult<bool>> AddAsync(CompanyUser companyUser)
        {
            var result = new TransactionResult<bool>();
            try
            {
                using (var db = new CrmContext())
                {
                    var data = db.Add(companyUser);
                    await db.SaveChangesAsync();
                    result.Data = true;                    
                }             
            }
            catch (Exception ex) {
                result.AddError(ex.Message);
                Console.WriteLine(ex.Message);
            }
            return result;
        }

        public async Task AddAsync(CompanyUserDto companyUser)
        {
            using (var db = new CrmContext())
            {
                var data = db.Add(companyUser);
                await db.SaveChangesAsync();
            }
        }
       
    }
}
