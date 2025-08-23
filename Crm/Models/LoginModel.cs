using System.ComponentModel.DataAnnotations;

namespace Crm.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Не указан Email")]
        public string Email { get; set; }
       
    }
}
