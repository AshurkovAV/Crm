
namespace Crm.Core.Features.Email.Models
{
    public class EmailVerificationRequest
    {
        public string Email { get; set; }
        public string VerificationToken { get; set; }
        public DateTime Expiration { get; set; }
    }
}
