using System.Security.Claims;

namespace Crm.Application.Interfaces
{
    public interface IUserContextService
    {
        int GetCurrentUserId();
        string GetCurrentUserName();
        bool IsUserInRole(string role);
        IEnumerable<Claim> GetUserClaims();
    }
}
