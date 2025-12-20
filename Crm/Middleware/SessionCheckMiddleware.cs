using Crm.Extensions;

namespace Crm.Middleware
{

    public class SessionCheckMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.ToString().ToLower();

            Console.WriteLine($"Middleware checking: {path}");

            // Расширенный список публичных путей
            if (IsPublicPath(path))
            {
                Console.WriteLine($"Public path: {path} - SKIPPING check");
                await _next(context);
                return;
            }

            // Проверяем авторизацию
            var isLoggedIn = context.Session.IsUserLoggedIn();
            Console.WriteLine($"Session check for {path}: {isLoggedIn}");

            if (!isLoggedIn)
            {
                Console.WriteLine($"Redirecting to login from: {path}");
                context.Response.Redirect("/Account/Login?returnUrl=" + Uri.EscapeDataString(context.Request.Path));
                return;
            }

            Console.WriteLine($"User authorized, continuing to: {path}");
            await _next(context);
        }

        private bool IsPublicPath(string path)
        {
            var publicPaths = new[]
            {
            "/account/login",
            "/account/register",
            "/account/forgotpassword",
            "/account/logout",
            "/account/", // все actions Account controller
            "/error",
            "/home/error",
            "/test/", // тестовые endpoints
            "/css/",
            "/js/",
            "/lib/",
            "/images/",
            "/fonts/",
            "/favicon.ico",
            "/.well-known/",
            "/api/", // если есть API
            "/swagger/", // если есть Swagger
            "/health" // health checks
        };

            var isPublic = publicPaths.Any(publicPath => path.StartsWith(publicPath));

            // Также исключаем корневой путь если он ведет на логин
            //if (path == "/" && context.Request.Query.ContainsKey("returnUrl"))
           // {
            //    isPublic = true;
            //}

            return isPublic;
        }
    }


    //public class SessionCheckMiddleware
    //{
    //    private readonly RequestDelegate _next;

    //    public SessionCheckMiddleware(RequestDelegate next)
    //    {
    //        _next = next;
    //    }

    //    public async Task InvokeAsync(HttpContext context)
    //    {
    //        var path = context.Request.Path;
    //        Console.WriteLine($"Processing path: {path}");

    //        if (IsExcludedPath(path))
    //        {
    //            Console.WriteLine($"Path {path} is excluded from session check");
    //            await _next(context);
    //            return;
    //        }

    //        if (!IsSessionAlive(context))
    //        {
    //            Console.WriteLine($"Session is not alive, redirecting to login from {path}");
    //            context.Response.Redirect("/Account/Login?returnUrl=" +
    //                Uri.EscapeDataString(context.Request.Path));
    //            return;
    //        }

    //        Console.WriteLine($"Session is alive, continuing to {path}");
    //        await _next(context);
    //    }

    //    private bool IsExcludedPath(PathString path)
    //    {
    //        var excludedPaths = new[]
    //        {
    //            "/Account/Login",    // Ваш реальный путь
    //            "/Account/Register",
    //            "/Account/ForgotPassword",
    //            "/Account/Logout",
    //            "/Error",
    //            "/Home/Error",
    //            "/css/",
    //            "/js/",
    //            "/lib/",
    //            "/images/",
    //            "/favicon.ico"
    //        };

    //        return excludedPaths.Any(excludedPath =>
    //            path.StartsWithSegments(excludedPath) ||
    //            path.Equals(excludedPath));
    //    }

    //    private bool IsSessionAlive(HttpContext context)
    //    {
    //        try
    //        {
    //            var session = context.Session;

    //            if (!session.IsAvailable)
    //                return false;

    //            session.LoadAsync().Wait();
    //            var user = session.GetCurrentUser();
    //            return user != null;
    //        }
    //        catch
    //        {
    //            return false;
    //        }
    //    }
    //}
}
