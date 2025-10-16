using Crm.Core.Features.Email.Interfaces;
using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Entity.Services
{
    public class VerificationTokenRepository : IVerificationTokenRepository
    {
     
        private readonly ILogger<VerificationTokenRepository> _logger;

        public VerificationTokenRepository(ILogger<VerificationTokenRepository> logger)
        {           
            _logger = logger;
        }

        public async Task<VerificationToken> CreateAsync(string email, TimeSpan expiration)
        {
            var token = GenerateSecureToken();
            var verificationToken = new VerificationToken
            {
                Email = email,
                Token = token,
                Expiration = DateTime.UtcNow.Add(expiration),
                IsUsed = false
            };
            using (var db = new CrmContext())
            {
                db.VerificationTokens.Add(verificationToken);
                await db.SaveChangesAsync();

                return verificationToken;
            }
            
        }

        public async Task<VerificationToken> GetValidTokenAsync(string email, string token)
        {
            using (var db = new CrmContext())
            {
                return await db.VerificationTokens
                .Where(vt => vt.Email == email
                          && vt.Token == token
                          && vt.Expiration > DateTime.UtcNow
                          && !vt.IsUsed)
                .OrderByDescending(vt => vt.CreatedAt)
                .FirstOrDefaultAsync();
            }
            
        }

        public async Task<bool> InvalidateTokenAsync(int tokenId)
        {
            using (var db = new CrmContext())
            {
                var token = await db.VerificationTokens.FindAsync(tokenId);
                if (token != null)
                {
                    token.IsUsed = true;
                    await db.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            
        }

        public async Task CleanupExpiredTokensAsync()
        {
            using (var db = new CrmContext())
            {
                var expiredTokens = await db.VerificationTokens
                .Where(vt => vt.Expiration <= DateTime.UtcNow || vt.IsUsed)
                .ToListAsync();

                db.VerificationTokens.RemoveRange(expiredTokens);
                await db.SaveChangesAsync();
            }
            
        }

        private string GenerateSecureToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var tokenData = new byte[32];
            rng.GetBytes(tokenData);
            return Convert.ToBase64String(tokenData)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}
