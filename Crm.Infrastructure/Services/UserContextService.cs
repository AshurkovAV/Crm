    using Crm.Application.Interfaces;
    using Microsoft.AspNetCore.Http;
    using System.Security.Claims;

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
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

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
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

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
                    .FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
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
