using Crm.Entity.ModelsCrm;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crm.Models
{
    public class UserProfileViewModel
    {
        // Информация о пользователе
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public string RealName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime? Birthday { get; set; }
        public string Sex { get; set; }
        public string Role { get; set; }

        // Компании пользователя
        public List<UserCompanyInfo> Companies { get; set; }

        // Текущая компания
        public int? CurrentCompanyId { get; set; }
        public UserCompanyInfo CurrentCompany { get; set; }
    }

    public class UserCompanyInfo
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string CompanyDescription { get; set; }
        public string UserRole { get; set; } // Роль в этой компании
        public string Position { get; set; }
        public DateTime? JoinedDate { get; set; }
        public bool IsOwner { get; set; } // Является ли владельцем компании
        public bool IsActive { get; set; }
    }
}
