using Crm.Application.Features.Accounts.DTOs;

namespace Crm.Application.Features.Accounts.Services
{
    public interface IYandexAuthService
    {       
        Task<YandexTokenResponse> GetTokenAsync(string code);
        Task<YandexUserInfoResponse> GetUserInfoAsync(string accessToken);
    }
}
