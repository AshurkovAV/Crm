using Crm.Application;
using Crm.Application.Interfaces;
using Crm.Core.Features.Account.Interfaces;
using Crm.Core.Features.Email.Interfaces;
using Crm.Core.Implementations;
using Crm.Core.Interfaces;
using Crm.Core.Services;
using Crm.Entity.Infrastructure.Services;
using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using Crm.Infrastructure.Services;
using Crm.Services.Email;
using Crm.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

// Регистрация репозиториев и сервисов
builder.Services.AddSingleton<ICrmRepository,            CrmRepository>();
builder.Services.AddSingleton<IUserRepository,           UserRepository>();
builder.Services.AddSingleton<INsiRepository,            NsiRepository>();
builder.Services.AddSingleton<IInvitationRepository,     InvitationRepository>();
builder.Services.AddSingleton<ICompanyRepository,        CompanyRepository>();
builder.Services.AddSingleton<IChatRepository,           ChatRepository>();
builder.Services.AddSingleton<IDealRepository,            DealRepository>();
builder.Services.AddSingleton<IClientRepository,          ClientRepository>();
builder.Services.AddSingleton<ISupplierRepository,        SupplierRepository>();
builder.Services.AddSingleton<IComponentRepository,       ComponentRepository>();
builder.Services.AddSingleton<IProductTemplateRepository, ProductTemplateRepository>();
builder.Services.AddSingleton<IProductTemplateComponentRepository, ProductTemplateComponentRepository>();
builder.Services.AddSingleton<IOrderItemRepository,       OrderItemRepository>();
builder.Services.AddSingleton<ICalculationService,        CalculationService>();
builder.Services.AddSingleton<ChatTypingStore>();
builder.Services.AddScoped<IVerificationTokenRepository, VerificationTokenRepository>();
builder.Services.AddScoped<IRememberDeviceService,       RememberDeviceService>();
builder.Services.AddScoped<IProfileService,              ProfileService>();
builder.Services.AddScoped<IUserContextService,          UserContextService>();
builder.Services.AddScoped<IAuthenticationService,       AuthenticationService>();

builder.Services.AddScoped<IInvitationService,           InvitationService>();
builder.Services.AddScoped<IEmailService,                EmailService>();
builder.Services.AddScoped<UserService>();

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
        options.Cookie.SameSite = SameSiteMode.Lax; // Защита от CSRF

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

using (var scope = app.Services.CreateScope())
{
    try
    {
        await using var db = new CrmContext();
        await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[ChatMessage]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatMessage]
    (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ChatMessage] PRIMARY KEY,
        [CompanyId] INT NOT NULL,
        [SenderUserId] INT NOT NULL,
        [RecipientUserId] INT NOT NULL,
        [Text] NVARCHAR(4000) NOT NULL,
        [SentAt] DATETIME2 NOT NULL CONSTRAINT [DF_ChatMessage_SentAt] DEFAULT (GETUTCDATE()),
        [IsRead] BIT NOT NULL CONSTRAINT [DF_ChatMessage_IsRead] DEFAULT (0),
        CONSTRAINT [FK_ChatMessage_Company] FOREIGN KEY ([CompanyId]) REFERENCES [dbo].[Company] ([Id]),
        CONSTRAINT [FK_ChatMessage_Sender] FOREIGN KEY ([SenderUserId]) REFERENCES [dbo].[Users] ([Id]),
        CONSTRAINT [FK_ChatMessage_Recipient] FOREIGN KEY ([RecipientUserId]) REFERENCES [dbo].[Users] ([Id])
    );
    CREATE INDEX [IX_ChatMessage_Conversation] ON [dbo].[ChatMessage]
        ([CompanyId], [SenderUserId], [RecipientUserId], [SentAt]);
END");
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception, "Не удалось проверить таблицу внутренних сообщений");
    }

    try
    {
        await using var db = new CrmContext();
        await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[Deal]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Deal]
    (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Deal] PRIMARY KEY,
        [CompanyId] INT NOT NULL,
        [OwnerId] INT NOT NULL,
        [Title] NVARCHAR(255) NOT NULL,
        [ClientName] NVARCHAR(255) NULL,
        [Amount] DECIMAL(18, 2) NOT NULL CONSTRAINT [DF_Deal_Amount] DEFAULT (0),
        [Status] NVARCHAR(50) NOT NULL CONSTRAINT [DF_Deal_Status] DEFAULT (N'Новая'),
        [ExpectedCloseDate] DATETIME2 NULL,
        [Description] NVARCHAR(2000) NULL,
        [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_Deal_CreatedDate] DEFAULT (GETUTCDATE()),
        [ModifiedDate] DATETIME2 NOT NULL CONSTRAINT [DF_Deal_ModifiedDate] DEFAULT (GETUTCDATE()),
        CONSTRAINT [FK_Deal_Company] FOREIGN KEY ([CompanyId]) REFERENCES [dbo].[Company] ([Id]),
        CONSTRAINT [FK_Deal_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [dbo].[Users] ([Id])
    );
    CREATE INDEX [IX_Deal_Company_Status_Modified] ON [dbo].[Deal]
        ([CompanyId], [Status], [ModifiedDate]);
END");
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception, "Не удалось проверить таблицу сделок");
    }

    try
    {
        await using var db = new CrmContext();
        await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[Deal]', N'U') IS NOT NULL
AND COL_LENGTH(N'[dbo].[Deal]', N'ClientId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Deal] ADD [ClientId] INT NULL;
    ALTER TABLE [dbo].[Deal] ADD CONSTRAINT [FK_Deal_Client]
        FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([ClientID]) ON DELETE SET NULL;
END");
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception, "Не удалось обновить связь сделок с контактами");
    }
}

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