namespace Crm.Services.Email
{
    public interface IEmailService
    {
        Task SendInvitationEmailAsync(string email, string link, string customMessage = null, string companyName = "Crm System");        
    }
}
