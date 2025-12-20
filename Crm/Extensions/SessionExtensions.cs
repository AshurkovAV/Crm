using Crm.Entity.ModelsCrm;
using System.Text.Json;

namespace Crm.Extensions
{
    public static class SessionExtensions
    {
        private const string UserSessionKey = "CurrentUser";

        public static void SetCurrentUser(this ISession session, User user)
        {
            try
            {
                Console.WriteLine("=== SETTING USER ===");

                // Создаем упрощенный объект для сессии (без циклических ссылок)
                var userSession = new
                {
                    user.Id,
                    user.DefaultEmail,
                    user.Login,
                    user.DisplayName,
                    user.FirstName,
                    user.LastName,
                    user.Role,
                    user.IsActive
                };

                var userJson = JsonSerializer.Serialize(userSession);
                session.SetString(UserSessionKey, userJson);

                Console.WriteLine($"User saved: {user.DefaultEmail}");
                Console.WriteLine($"Session ID: {session.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR setting user: {ex.Message}");
            }
        }

        public static User? GetCurrentUser(this ISession session)
        {
            try
            {
                if (!session.IsAvailable)
                {
                    Console.WriteLine("Session not available");
                    return null;
                }

                var userData = session.GetString(UserSessionKey);

                if (string.IsNullOrEmpty(userData))
                {
                    Console.WriteLine("No user data in session");
                    return null;
                }

                var user = JsonSerializer.Deserialize<User>(userData);
                Console.WriteLine($"User retrieved: {user?.DefaultEmail}");
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR getting user: {ex.Message}");
                return null;
            }
        }
        // Метод для проверки живой сессии
        public static bool IsUserLoggedIn(this ISession session)
        {
            var user = session.GetCurrentUser();
            return user != null && user.IsActive;
        }
    }
}
