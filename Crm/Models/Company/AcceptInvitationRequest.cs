using System.ComponentModel.DataAnnotations;

namespace Crm.Models.Company
{
    public class AcceptInvitationRequest
    {
        [Required(ErrorMessage = "Имя обязательно")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Имя должно быть от 2 до 100 символов")]
        [Display(Name = "Имя и фамилия")]
        public string DisplayName { get; set; }

        [EmailAddress(ErrorMessage = "Некорректный email адрес")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Некорректный номер телефона")]
        [Display(Name = "Телефон (необязательно)")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть не менее 6 символов")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Подтверждение пароля")]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; }
    }
}
