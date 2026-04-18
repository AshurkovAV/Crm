
using Crm.Core.Infrastructure;
using Crm.Entity.DTO;
using Crm.Entity.ModelsCrm;

namespace Crm.Entity.Services
{
    public interface ICompanyRepository
    {
        /// <summary>
        /// Получить всех пользователей компании с их ролями
        /// </summary>
        Task<List<CompanyUserDto>> GetCompanyUsersAsync(int companyId, int currentUserId);
        Task<List<CompanyUserDto>> GetCompanyUsersAsync(int companyId);
        Task<TransactionResult<int>> GetCompanyIdToUserId(int userId);
        Task<TransactionResult<CompanyUser>> GetCompanyUserToUserId(int userId, int? conmanyId);
        Task AddOrUpdateAsync(CompanyUser companyUser);
        Task<List<CompanyUserDto>> GetCompanyUsersByUserIdAsync(int userId);
        Task<bool> UserHasAnyCompanyAsync(int userId);
        Task<bool> UserHasOwnCompanyAsync(int userId);
        Task AddAsync(Company company);
        Task<TransactionResult<bool>> AddAsync(CompanyUser companyUser);
    }
}