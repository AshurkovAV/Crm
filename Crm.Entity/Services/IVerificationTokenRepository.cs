using Crm.Entity.ModelsCrm;

namespace Crm.Core.Features.Email.Interfaces
{
    public interface IVerificationTokenRepository
    {
        Task<VerificationToken> CreateAsync(string email, TimeSpan expiration);
        Task<VerificationToken> GetValidTokenAsync(string email, string token);
        Task<bool> InvalidateTokenAsync(int tokenId);
        Task CleanupExpiredTokensAsync();
    }
}
