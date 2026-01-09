using Crm.Application;
using Crm.Application.Interfaces;
using Crm.Core.Features.Account.Interfaces;
using Crm.Core.Features.Email.Interfaces;
using Crm.Core.Implementations;
using Crm.Core.Interfaces;
using Crm.Core.Services;
using Crm.Entity.Infrastructure.Services;
using Crm.Entity.Services;
using Crm.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

// Регистрация репозиториев и сервисов
builder.Services.AddSingleton<ICrmRepository,  CrmRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddScoped<IVerificationTokenRepository, VerificationTokenRepository>();
builder.Services.AddScoped<IRememberDeviceService,       RememberDeviceService>();
builder.Services.AddScoped<IProfileService,              ProfileService>();
builder.Services.AddScoped<IUserContextService,          UserContextService>();
builder.Services.AddScoped<IAuthenticationService,       AuthenticationService>();

builder.Services.AddApplication();

// ====== КОНФИГУРАЦИЯ АУТЕНТИФИКАЦИИ ЧЕРЕЗ КУКИ ======
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Пути
        options.LoginPath = "/Account/Index";
        options.AccessDeniedPath = "/Error/401";
        options.LogoutPath = "/Account/Logout";

        // Настройки куки
        options.Cookie.Name = ".AspNetCore.Crm.Auth"; // Уникальное имя
        options.Cookie.HttpOnly = true; // Защита от XSS
        options.Cookie.SameSite = SameSiteMode.Strict; // Защита от CSRF

        // ВРЕМЯ ЖИЗНИ и БЕЗОПАСНОСТЬ
        options.ExpireTimeSpan = TimeSpan.FromDays(7); // Авторизация на 7 дней
        options.SlidingExpiration = true; // Обновлять срок при активности

        // HTTPS (важно!)
        if (builder.Environment.IsDevelopment())
        {
            // В разработке можно SameAsRequest если нет HTTPS
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        }
        else
        {
            // В продакшене ВСЕГДА HTTPS
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        }

        // Обработка AJAX-запросов
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                // Если это AJAX-запрос, возвращаем 401 вместо редиректа
                if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                    context.Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                }

                // Обычный запрос - стандартный редирект
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },

            // Можно добавить дополнительные обработчики
            OnValidatePrincipal = async context =>
            {
                // Здесь можно добавить дополнительную валидацию
                // Например, проверку блокировки пользователя
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // При необходимости добавляем политики
    // options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // HTTP Strict Transport Security
}

app.UseStaticFiles();
app.UseRouting();

// ВАЖНО: этот порядок!
app.UseAuthentication(); // Сначала аутентификация
app.UseAuthorization();  // Затем авторизация

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();