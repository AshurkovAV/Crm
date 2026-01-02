using Crm.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Core.Services
{
    public class UserContextService : IUserContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContextService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim) ||
                !int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found");
            }

            return userId;
        }

        public int? TryGetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim) ||
                !int.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            return userId;
        }

        public string GetCurrentUserName()
        {
            return _httpContextAccessor.HttpContext?.User?
                .FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        }

        public bool IsUserInRole(string role)
        {
            return _httpContextAccessor.HttpContext?.User?
                .IsInRole(role) ?? false;
        }

        public IEnumerable<Claim> GetUserClaims()
        {
            return _httpContextAccessor.HttpContext?.User?.Claims ??
                   Enumerable.Empty<Claim>();
        }
    }
}
