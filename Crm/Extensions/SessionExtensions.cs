using Crm.Entity.ModelsCrm;
using System.Text.Json;

namespace Crm.Extensions
{
    public static class SessionExtensions
    {
        private const string UserSessionKey = "CurrentUser";

        public static void SetCurrentUser(this ISession session, User user)
        {
            Console.WriteLine("Сюда пришли 4");
            session.SetString(UserSessionKey, JsonSerializer.Serialize(user));
        }

        public static User? GetCurrentUser(this ISession session)
        {
            var userData = session.GetString(UserSessionKey);
            return userData == null ? null : JsonSerializer.Deserialize<User>(userData);
        }
    }
}
