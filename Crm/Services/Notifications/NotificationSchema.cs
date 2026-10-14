namespace Crm.Services.Notifications
{
    /// <summary>
    /// Таблицы модуля уведомлений. Применяются при старте приложения (Program.cs) идемпотентно,
    /// как и остальные аддитивные изменения схемы.
    /// </summary>
    public static class NotificationSchema
    {
        public static readonly IReadOnlyList<(string Table, string Sql)> Scripts = new List<(string, string)>
        {
            ("NotificationOutbox", @"
IF OBJECT_ID(N'[dbo].[NotificationOutbox]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[NotificationOutbox]
    (
        [Id]            BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_NotificationOutbox] PRIMARY KEY,
        [CompanyId]     INT NULL,
        [UserId]        INT NOT NULL,
        [Channel]       NVARCHAR(20) NOT NULL,
        [Kind]          NVARCHAR(40) NOT NULL,
        [Subject]       NVARCHAR(300) NOT NULL,
        [BodyHtml]      NVARCHAR(MAX) NULL,
        [BodyText]      NVARCHAR(MAX) NOT NULL,
        [LinkUrl]       NVARCHAR(500) NULL,
        [LinkText]      NVARCHAR(100) NULL,
        [Status]        NVARCHAR(20) NOT NULL CONSTRAINT [DF_NotificationOutbox_Status] DEFAULT (N'Pending'),
        [Attempts]      INT NOT NULL CONSTRAINT [DF_NotificationOutbox_Attempts] DEFAULT (0),
        [NextAttemptAt] DATETIME2 NOT NULL CONSTRAINT [DF_NotificationOutbox_NextAttemptAt] DEFAULT (SYSUTCDATETIME()),
        [CreatedAt]     DATETIME2 NOT NULL CONSTRAINT [DF_NotificationOutbox_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [SentAt]        DATETIME2 NULL,
        [LastError]     NVARCHAR(500) NULL,
        CONSTRAINT [FK_NotificationOutbox_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id])
    );
    CREATE INDEX [IX_NotificationOutbox_Status_Next] ON [dbo].[NotificationOutbox]([Status], [NextAttemptAt]);
END"),

            ("NotificationPreference", @"
IF OBJECT_ID(N'[dbo].[NotificationPreference]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[NotificationPreference]
    (
        [UserId]       INT NOT NULL CONSTRAINT [PK_NotificationPreference] PRIMARY KEY,
        [TaskEmail]    BIT NOT NULL CONSTRAINT [DF_NotificationPreference_TaskEmail] DEFAULT (1),
        [TaskMax]      BIT NOT NULL CONSTRAINT [DF_NotificationPreference_TaskMax] DEFAULT (1),
        [WikiEmail]    BIT NOT NULL CONSTRAINT [DF_NotificationPreference_WikiEmail] DEFAULT (1),
        [WikiMax]      BIT NOT NULL CONSTRAINT [DF_NotificationPreference_WikiMax] DEFAULT (1),
        [ModifiedDate] DATETIME2 NOT NULL CONSTRAINT [DF_NotificationPreference_ModifiedDate] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [FK_NotificationPreference_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
    );
END"),

            ("UserMaxLink", @"
IF OBJECT_ID(N'[dbo].[UserMaxLink]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserMaxLink]
    (
        [UserId]             INT NOT NULL CONSTRAINT [PK_UserMaxLink] PRIMARY KEY,
        [MaxUserId]          BIGINT NULL,
        [LinkedAt]           DATETIME2 NULL,
        [LinkTokenHash]      NVARCHAR(64) NULL,
        [LinkTokenExpiresAt] DATETIME2 NULL,
        CONSTRAINT [FK_UserMaxLink_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_UserMaxLink_MaxUserId] ON [dbo].[UserMaxLink]([MaxUserId]);
    CREATE INDEX [IX_UserMaxLink_LinkTokenHash] ON [dbo].[UserMaxLink]([LinkTokenHash]);
END"),

            ("WikiNotificationEvent", @"
IF OBJECT_ID(N'[dbo].[WikiNotificationEvent]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WikiNotificationEvent]
    (
        [Id]           BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_WikiNotificationEvent] PRIMARY KEY,
        [CompanyId]    INT NOT NULL,
        [PageId]       INT NOT NULL,
        [AuthorUserId] INT NOT NULL,
        [Kind]         NVARCHAR(20) NOT NULL CONSTRAINT [DF_WikiNotificationEvent_Kind] DEFAULT (N'Created'),
        [CreatedAt]    DATETIME2 NOT NULL CONSTRAINT [DF_WikiNotificationEvent_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [ProcessedAt]  DATETIME2 NULL,
        CONSTRAINT [FK_WikiNotificationEvent_Page] FOREIGN KEY ([PageId]) REFERENCES [dbo].[WikiPage] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_WikiNotificationEvent_Company_Processed] ON [dbo].[WikiNotificationEvent]([CompanyId], [ProcessedAt]);
END"),
        };
    }
}
