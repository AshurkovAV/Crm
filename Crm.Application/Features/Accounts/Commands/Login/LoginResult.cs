namespace Crm.Application.Features.Accounts.Commands.Login
{
    public class LoginResult
    {
        public bool Succeeded { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public List<string> Errors { get; set; } = new();

        // Factory methods для удобства
        public static LoginResult Success(string token, string refreshToken, string userId, string email)
        {
            return new LoginResult
            {
                Succeeded = true,
                Token = token,
                RefreshToken = refreshToken,
                UserId = userId,
                Email = email
            };
        }

        public static LoginResult Failure(params string[] errors)
        {
            return new LoginResult
            {
                Succeeded = false,
                Errors = errors.ToList()
            };
        }
    }
}
