namespace Crm.Middleware
{
    public class DebugCookieMiddleware
    {
        private readonly RequestDelegate _next;

        public DebugCookieMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sessionCookie = context.Request.Cookies["Crm.Session"];
            Console.WriteLine($"=== COOKIE DEBUG ===");
            Console.WriteLine($"Request Cookie: {sessionCookie ?? "NULL"}");
            Console.WriteLine($"Session ID: {context.Session.Id}");
            Console.WriteLine($"Path: {context.Request.Path}");
            Console.WriteLine($"=== END COOKIE DEBUG ===");

            await _next(context);

            // Проверяем, установлена ли кука в ответе
            var hasSetCookie = context.Response.Headers.ContainsKey("Set-Cookie");
            Console.WriteLine($"=== RESPONSE COOKIE ===");
            Console.WriteLine($"Set-Cookie exists: {hasSetCookie}");
            if (hasSetCookie)
            {
                foreach (var cookieHeader in context.Response.Headers["Set-Cookie"])
                {
                    Console.WriteLine($"Set-Cookie: {cookieHeader}");
                }
            }
            Console.WriteLine($"=== END RESPONSE COOKIE ===");
        }
    }
}
