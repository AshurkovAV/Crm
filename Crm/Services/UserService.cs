using Crm.Models;
using Crm.Entity.Services;
using Crm.Models.Profil;

namespace Crm.Services
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICompanyRepository _companyRepository;

        public UserService(
            IUserRepository userRepository,
            ICompanyRepository companyRepository)
        {
            _userRepository = userRepository;
            _companyRepository = companyRepository;
        }

        public UserProfileViewModel GetUserProfile(int userId)
        {
            // Получаем данные из репозитория (только entity)
            var user = _companyRepository.GetUserWithCompanies(userId);

            if (user == null)
                return null;

            // Получаем активные компании пользователя
            var activeCompanies = _companyRepository.GetUserActiveCompanies(userId);

            // Маппинг entity -> ViewModel (на уровне App)
            var profile = new UserProfileViewModel
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                RealName = user.RealName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.DefaultEmail,
                Phone = user.DefaultPhone,
                Birthday = user.Birthday,
                Sex = user.Sex,
                Role = user.Role,
                Companies = activeCompanies.Select(cu => new UserCompanyInfo
                {
                    CompanyId = cu.CompanyId,
                    CompanyName = cu.Company?.Name,
                    CompanyDescription = cu.Company?.Description,
                    UserRole = cu.Role,
                    Position = cu.Position,
                    JoinedDate = cu.JoinedDate,
                    IsOwner = cu.Company?.OwnerId == userId,
                    IsActive = cu.IsActive 
                }).ToList(),
                CurrentCompanyId = user.CurrentCompanyId
            };

            // Находим текущую компанию
            if (user.CurrentCompanyId.HasValue)
            {
                profile.CurrentCompany = profile.Companies
                    .FirstOrDefault(c => c.CompanyId == user.CurrentCompanyId.Value);
            }

            return profile;
        }

        public bool SwitchCurrentCompany(int userId, int companyId)
        {
            return _companyRepository.UpdateUserCurrentCompany(userId, companyId);
        }

        public async Task<bool> UpdateProfileAsync(int userId, EditProfileModel model)
        {
            try
            {
                var user = _userRepository.GetUserById(userId).Data;
                if (user == null)
                    return false;

                // Основная информация
                user.DisplayName = model.DisplayName;
                user.RealName = model.RealName;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.MiddleName = model.MiddleName;
                user.Sex = model.Sex;
                user.BirthPlace = model.BirthPlace;

                // Контакты
                user.DefaultEmail = model.DefaultEmail;
                user.AlternativeEmail = model.AlternativeEmail;
                user.DefaultPhone = model.DefaultPhone;
                user.AlternativePhone = model.AlternativePhone;
                user.WorkPhone = model.WorkPhone;

                // Мессенджеры
                user.Telegram = model.Telegram;
                user.WhatsApp = model.WhatsApp;

                // Работа
                user.Position = model.Position;
                user.Department = model.Department;

                // Личное
                user.Address = model.Address;
                user.Bio = model.Bio;
                user.Website = model.Website;
                user.LinkedIn = model.LinkedIn;

                // Дата рождения
                if (!string.IsNullOrEmpty(model.Birthday))
                {
                    if (DateTime.TryParse(model.Birthday, out DateTime birthday))
                        user.Birthday = birthday;
                }
                else
                {
                    user.Birthday = null;
                }

                user.ModifiedDate = DateTime.UtcNow;

                var result = _userRepository.AddOrUpdateAsync(user);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating profile: {ex.Message}");
                return false;
            }
        }
    }
}