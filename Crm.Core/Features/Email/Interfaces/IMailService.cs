using Crm.Core.Features.Email.Models;

namespace Crm.Core.Features.Email.Interfaces
{
    public interface IMailService
    {
        Task<bool> SendEmailAsync(Mail mail);
        Task<bool> SendVerificationEmailAsync(string email, string verificationToken);
    }
}
