using Microsoft.AspNetCore.Http;
using Crm.Entity.ModelsCrm;

namespace Crm.Application.Interfaces
{
    public interface IAuthenticationService
    {
        Task CreateRememberTokenAsync(string email, int userid);
        Task ClearRememberTokenAsync(string email);
        Task AuthenticateWithCookiesAsync( User user);
        Task SignOutAsync();
        Task UpdateRememberCookiesAsync(string email);
    }
}
