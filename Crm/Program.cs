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
builder.Services.AddSingleton<IPublicTrackingRepository,  PublicTrackingRepository>();
builder.Services.AddSingleton<IProductionTaskRepository,  ProductionTaskRepository>();
builder.Services.AddSingleton<IContractorRepository,           ContractorRepository>();
builder.Services.AddSingleton<IContractorAssignmentRepository, ContractorAssignmentRepository>();
builder.Services.AddSingleton<IContractorPortalRepository,     ContractorPortalRepository>();
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

    // ====== Производственный модуль (ТЗ "BigLV: Прозрачный завод") ======
    // Только аддитивные, идемпотентные изменения (CREATE IF NOT EXISTS / ADD COLUMN IF NOT EXISTS).
    // Удаление старой демо-схемы склада делается вручную: Crm.Entity/Migrations/0001_production_schema.sql

    try
    {
        await using var db = new CrmContext();
        await db.Database.ExecuteSqlRawAsync(@"
IF COL_LENGTH(N'[dbo].[Clients]', N'CompanyId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Clients] ADD [CompanyId] INT NULL;
    ALTER TABLE [dbo].[Clients] ADD CONSTRAINT [FK_Clients_Company]
        FOREIGN KEY ([CompanyId]) REFERENCES [dbo].[Company] ([Id]);
    CREATE INDEX [IX_Clients_CompanyId] ON [dbo].[Clients]([CompanyId]);
END");
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception, "Не удалось добавить CompanyId в Clients");
    }

    try
    {
        await using var db = new CrmContext();
        await db.Database.ExecuteSqlRawAsync(@"
IF COL_LENGTH(N'[dbo].[Deal]', N'InstallationAddress') IS NULL
BEGIN
    ALTER TABLE [dbo].[Deal] ADD [InstallationAddress] NVARCHAR(500) NULL;
    ALTER TABLE [dbo].[Deal] ADD [PrepaymentAmount] DECIMAL(18, 2) NULL;
    ALTER TABLE [dbo].[Deal] ADD [PublicToken] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID();
    CREATE UNIQUE INDEX [UQ_Deal_PublicToken] ON [dbo].[Deal]([PublicToken]);
END");
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception, "Не удалось добавить поля адреса/предоплаты/публичного токена в Deal");
    }

    try
    {
        await using var db = new CrmContext();
        await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[Supplier]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Supplier]
    (
        [SupplierId]    INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Supplier] PRIMARY KEY,
        [CompanyId]     INT NOT NULL,
        [Name]          NVARCHAR(100) NOT NULL,
        [ContactPerson] NVARCHAR(100) NULL,
        [Phone]         NVARCHAR(20) NULL,
        [Email]         NVARCHAR(100) NULL,
        [Notes]         NVARCHAR(500) NULL,
        [IsActive]      BIT NOT NULL CONSTRAINT [DF_Supplier_IsActive] DEFAULT (1),
        CONSTRAINT [FK_Supplier_Company] FOREIGN KEY ([CompanyId]) REFERENCES [dbo].[Company] ([Id])
    );
    CREATE INDEX [IX_Supplier_CompanyId] ON [dbo].[Supplier]([CompanyId]);
END");
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception, "Не удалось создать таблицу Supplier");
    }

    try
    {
        await using var db = new CrmContext();
        await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[Component]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Component]
    (
        [ComponentId]   INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Component] PRIMARY KEY,
        [CompanyId]     INT NOT NULL,
        [Name]          NVARCHAR(200) NOT NULL,
        [Unit]          NVARCHAR(20) NOT NULL,
        [CostPrice]     DECIMAL(18, 2) NOT NULL,
        [StockQuantity] DECIMAL(18, 3) NOT NULL CONSTRAINT [DF_Component_StockQuantity] DEFAULT (0),
        [ReorderLevel]  DECIMAL(18, 3) NULL,
        [SupplierId]    INT NULL,
        [IsActive]      BIT NOT NULL CONSTRAINT [DF_Component_IsActive] DEFAULT (1),
        CONSTRAINT [FK_Component_Company] FOREIGN KEY ([CompanyId]) REFERENCES [dbo].[Company] ([Id]),
        CONSTRAINT [FK_Component_Supplier] FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[Supplier] ([SupplierId])
    );
    CREATE INDEX [IX_Component_CompanyId] ON [dbo].[Component]([CompanyId]);
END");
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception, "Не удалось создать таблицу Component");
    }

    try
    {
        await using var db = new CrmContext();
        await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[ProductTemplate]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductTemplate]
    (
        [ProductTemplateId]    INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ProductTemplate] PRIMARY KEY,
        [CompanyId]            INT NOT NULL,
        [Name]                 NVARCHAR(200) NOT NULL,
        [Category]             NVARCHAR(100) NULL,
        [Unit]                 NVARCHAR(20) NOT NULL,
        [FormulaExpression]    NVARCHAR(2000) NOT NULL,
        [DefaultMarginPercent] DECIMAL(5, 2) NULL,
        [IsActive]             BIT NOT NULL CONSTRAINT [DF_ProductTemplate_IsActive] DEFAULT (1),
        CONSTRAINT [FK_ProductTemplate_Company] FOREIGN KEY ([CompanyId]) REFERENCES [dbo].[Company] ([Id])
    );
    CREATE INDEX [IX_ProductTemplate_CompanyId] ON [dbo].[ProductTemplate]([CompanyId]);
END");
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception, "Не удалось создать таблицу ProductTemplate");
    }

    try
    {
        await using var db = new CrmContext();
        await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[ProductTemplateComponent]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductTemplateComponent]
    (
        [Id]                INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ProductTemplateComponent] PRIMARY KEY,
        [ProductTemplateId] INT NOT NULL,
        [ComponentId]       INT NOT NULL,
        [QuantityFormula]   NVARCHAR(500) NOT NULL,
        [Notes]             NVARCHAR(200) NULL,
        CONSTRAINT [FK_PTC_ProductTemplate] FOREIGN KEY ([ProductTemplateId]) REFERENCES [dbo].[ProductTemplate] ([ProductTemplateId]) ON DELETE CASCADE,
        CONSTRAINT [FK_PTC_Component] FOREIGN KEY ([ComponentId]) REFERENCES [dbo].[Component] ([ComponentId])
    );
    CREATE INDEX [IX_ProductTemplateComponent_ProductTemplateId] ON [dbo].[ProductTemplateComponent]([ProductTemplateId]);
END");
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception, "Не удалось создать таблицу ProductTemplateComponent");
    }

    try
    {
        await using var db = new CrmContext();
        await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[OrderItem]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OrderItem]
    (
        [OrderItemId]       INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_OrderItem] PRIMARY KEY,
        [DealId]            INT NOT NULL,
        [ProductTemplateId] INT NULL,
        [Name]              NVARCHAR(255) NOT NULL,
        [Quantity]          DECIMAL(18, 3) NOT NULL CONSTRAINT [DF_OrderItem_Quantity] DEFAULT (1),
        [Width]             DECIMAL(18, 3) NULL,
        [Height]            DECIMAL(18, 3) NULL,
        [Depth]             DECIMAL(18, 3) NULL,
        [CostPrice]         DECIMAL(18, 2) NOT NULL CONSTRAINT [DF_OrderItem_CostPrice] DEFAULT (0),
        [MarginPercent]     DECIMAL(5, 2) NULL,
        [Price]             DECIMAL(18, 2) NOT NULL CONSTRAINT [DF_OrderItem_Price] DEFAULT (0),
        [Status]            NVARCHAR(50) NOT NULL CONSTRAINT [DF_OrderItem_Status] DEFAULT (N'Новое'),
        [CreatedDate]       DATETIME2 NOT NULL CONSTRAINT [DF_OrderItem_CreatedDate] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [FK_OrderItem_Deal] FOREIGN KEY ([DealId]) REFERENCES [dbo].[Deal] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrderItem_ProductTemplate] FOREIGN KEY ([ProductTemplateId]) REFERENCES [dbo].[ProductTemplate] ([ProductTemplateId])
    );
    CREATE INDEX [IX_OrderItem_DealId] ON [dbo].[OrderItem]([DealId]);
END");
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception, "Не удалось создать таблицу OrderItem");
    }

    try
    {
        await using var db = new CrmContext();
        await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[Contractor]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Contractor]
    (
        [ContractorId] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Contractor] PRIMARY KEY,
        [CompanyId]    INT NOT NULL,
        [Name]         NVARCHAR(200) NOT NULL,
        [Phone]        NVARCHAR(20) NULL,
        [Email]        NVARCHAR(100) NULL,
        [Notes]        NVARCHAR(500) NULL,
        [IsActive]     BIT NOT NULL CONSTRAINT [DF_Contractor_IsActive] DEFAULT (1),
        CONSTRAINT [FK_Contractor_Company] FOREIGN KEY ([CompanyId]) REFERENCES [dbo].[Company] ([Id])
    );
    CREATE INDEX [IX_Contractor_CompanyId] ON [dbo].[Contractor]([CompanyId]);
END");
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception, "Не удалось создать таблицу Contractor");
    }

    try
    {
        await using var db = new CrmContext();
        await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[ProductionTask]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductionTask]
    (
        [ProductionTaskId] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ProductionTask] PRIMARY KEY,
        [OrderItemId]      INT NOT NULL,
        [StageName]        NVARCHAR(100) NOT NULL,
        [StageOrder]       INT NOT NULL CONSTRAINT [DF_ProductionTask_StageOrder] DEFAULT (0),
        [ExecutorType]     NVARCHAR(20) NOT NULL CONSTRAINT [DF_ProductionTask_ExecutorType] DEFAULT (N'Internal'),
        [AssignedUserId]   INT NULL,
        [ContractorId]     INT NULL,
        [Status]           NVARCHAR(20) NOT NULL CONSTRAINT [DF_ProductionTask_Status] DEFAULT (N'Pending'),
        [StartedAt]        DATETIME2 NULL,
        [CompletedAt]      DATETIME2 NULL,
        [PhotoUrl]         NVARCHAR(500) NULL,
        [Notes]            NVARCHAR(2000) NULL,
        CONSTRAINT [FK_ProductionTask_OrderItem] FOREIGN KEY ([OrderItemId]) REFERENCES [dbo].[OrderItem] ([OrderItemId]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductionTask_AssignedUser] FOREIGN KEY ([AssignedUserId]) REFERENCES [dbo].[Users] ([Id]),
        CONSTRAINT [FK_ProductionTask_Contractor] FOREIGN KEY ([ContractorId]) REFERENCES [dbo].[Contractor] ([ContractorId])
    );
    CREATE INDEX [IX_ProductionTask_OrderItemId_StageOrder] ON [dbo].[ProductionTask]([OrderItemId], [StageOrder]);
END");
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception, "Не удалось создать таблицу ProductionTask");
    }

    try
    {
        await using var db = new CrmContext();
        await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[ContractorAccessToken]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ContractorAccessToken]
    (
        [ContractorAccessTokenId] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ContractorAccessToken] PRIMARY KEY,
        [ContractorId]            INT NOT NULL,
        [ProductionTaskId]        INT NOT NULL,
        [Token]                   UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ContractorAccessToken_Token] DEFAULT (NEWID()),
        [CreatedAt]               DATETIME2 NOT NULL CONSTRAINT [DF_ContractorAccessToken_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [ExpiresAt]               DATETIME2 NULL,
        [UsedAt]                  DATETIME2 NULL,
        CONSTRAINT [FK_CAT_Contractor] FOREIGN KEY ([ContractorId]) REFERENCES [dbo].[Contractor] ([ContractorId]) ON DELETE CASCADE,
        CONSTRAINT [FK_CAT_ProductionTask] FOREIGN KEY ([ProductionTaskId]) REFERENCES [dbo].[ProductionTask] ([ProductionTaskId])
    );
    CREATE UNIQUE INDEX [UQ_ContractorAccessToken_Token] ON [dbo].[ContractorAccessToken]([Token]);
END");
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception, "Не удалось создать таблицу ContractorAccessToken");
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