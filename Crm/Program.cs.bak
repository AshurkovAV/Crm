using Crm.Application;
using Crm.Core.Implementations;
using Crm.Core.Interfaces;
using Crm.Entity.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

// Внедряем зависимость
builder.Services.AddSingleton<ICrmRepository, CrmRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();


builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IProfileService, ProfileService>();

// Регистрируем все обработчики команд из Application слоя
builder.Services.AddApplication();

// Добавление сервисов аутентификации
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Index"; // Путь к странице входа
        options.AccessDeniedPath = "/Error/401"; // Путь к странице 401
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                context.Response.Redirect("/Account/Index");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();



// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();


app.MapGet("/Home/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/Account/Index");
});

// Изменяем маршрут по умолчанию на Account/Index
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();