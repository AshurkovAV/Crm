using Crm.Application;
using Crm.Core.Features.Account.Interfaces;
using Crm.Core.Features.Email.Interfaces;
using Crm.Core.Implementations;
using Crm.Core.Interfaces;
using Crm.Entity.Infrastructure.Services;
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
builder.Services.AddControllersWithViews().AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
builder.Services.AddHttpClient();

// �������� �����������
builder.Services.AddSingleton<ICrmRepository, CrmRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddScoped<IVerificationTokenRepository, VerificationTokenRepository>();
builder.Services.AddScoped<IRememberDeviceService, RememberDeviceService>();



builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IProfileService, ProfileService>();

// ������������ ��� ����������� ������ �� Application ����
builder.Services.AddApplication();

// ���������� �������� ��������������
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Index"; // ���� � �������� �����
        options.AccessDeniedPath = "/Error/401"; // ���� � �������� 401
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

// �������� ������� �� ��������� �� Account/Index
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();