namespace Crm.Services.Wiki
{
    /// <summary>
    /// Таблицы базы знаний. Применяются при старте приложения (Program.cs) так же, как остальные
    /// аддитивные изменения схемы: каждая проверка идемпотентна (IF OBJECT_ID ... IS NULL).
    /// Порядок важен: таблицы со ссылками идут после таблиц, на которые ссылаются.
    /// </summary>
    public static class WikiSchema
    {
        public static readonly IReadOnlyList<(string Table, string Sql)> Scripts = new List<(string, string)>
        {
            ("WikiSection", @"
IF OBJECT_ID(N'[dbo].[WikiSection]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WikiSection]
    (
        [Id]              INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_WikiSection] PRIMARY KEY,
        [CompanyId]       INT NOT NULL,
        [Name]            NVARCHAR(200) NOT NULL,
        [Icon]            NVARCHAR(16) NULL,
        [Description]     NVARCHAR(1000) NULL,
        [SortOrder]       INT NOT NULL CONSTRAINT [DF_WikiSection_SortOrder] DEFAULT (0),
        [AccessMode]      NVARCHAR(20) NOT NULL CONSTRAINT [DF_WikiSection_AccessMode] DEFAULT (N'All'),
        [CreatedByUserId] INT NOT NULL,
        [CreatedDate]     DATETIME2 NOT NULL CONSTRAINT [DF_WikiSection_CreatedDate] DEFAULT (SYSUTCDATETIME()),
        [ModifiedDate]    DATETIME2 NOT NULL CONSTRAINT [DF_WikiSection_ModifiedDate] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [FK_WikiSection_Company] FOREIGN KEY ([CompanyId]) REFERENCES [dbo].[Company] ([Id]),
        CONSTRAINT [FK_WikiSection_CreatedByUser] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[Users] ([Id])
    );
    CREATE INDEX [IX_WikiSection_CompanyId] ON [dbo].[WikiSection]([CompanyId]);
END"),

            ("WikiSectionAccess", @"
IF OBJECT_ID(N'[dbo].[WikiSectionAccess]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WikiSectionAccess]
    (
        [Id]          INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_WikiSectionAccess] PRIMARY KEY,
        [SectionId]   INT NOT NULL,
        [UserId]      INT NOT NULL,
        [GrantedDate] DATETIME2 NOT NULL CONSTRAINT [DF_WikiSectionAccess_GrantedDate] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [FK_WikiSectionAccess_Section] FOREIGN KEY ([SectionId]) REFERENCES [dbo].[WikiSection] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WikiSectionAccess_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id])
    );
    CREATE UNIQUE INDEX [UQ_WikiSectionAccess_Section_User] ON [dbo].[WikiSectionAccess]([SectionId], [UserId]);
END"),

            ("WikiEditor", @"
IF OBJECT_ID(N'[dbo].[WikiEditor]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WikiEditor]
    (
        [Id]              INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_WikiEditor] PRIMARY KEY,
        [CompanyId]       INT NOT NULL,
        [SectionId]       INT NULL,
        [UserId]          INT NOT NULL,
        [GrantedByUserId] INT NOT NULL,
        [GrantedDate]     DATETIME2 NOT NULL CONSTRAINT [DF_WikiEditor_GrantedDate] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [FK_WikiEditor_Company] FOREIGN KEY ([CompanyId]) REFERENCES [dbo].[Company] ([Id]),
        CONSTRAINT [FK_WikiEditor_Section] FOREIGN KEY ([SectionId]) REFERENCES [dbo].[WikiSection] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WikiEditor_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id])
    );
    CREATE UNIQUE INDEX [UQ_WikiEditor_Company_User_Section] ON [dbo].[WikiEditor]([CompanyId], [UserId], [SectionId]);
END"),

            ("WikiPage", @"
IF OBJECT_ID(N'[dbo].[WikiPage]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WikiPage]
    (
        [Id]               INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_WikiPage] PRIMARY KEY,
        [CompanyId]        INT NOT NULL,
        [SectionId]        INT NOT NULL,
        [ParentId]         INT NULL,
        [Title]            NVARCHAR(300) NOT NULL,
        [ContentJson]      NVARCHAR(MAX) NULL,
        [ContentHtml]      NVARCHAR(MAX) NULL,
        [ContentText]      NVARCHAR(MAX) NULL,
        [SortOrder]        INT NOT NULL CONSTRAINT [DF_WikiPage_SortOrder] DEFAULT (0),
        [IsPinned]         BIT NOT NULL CONSTRAINT [DF_WikiPage_IsPinned] DEFAULT (0),
        [Version]          INT NOT NULL CONSTRAINT [DF_WikiPage_Version] DEFAULT (1),
        [OwnerUserId]      INT NULL,
        [ReviewDueDate]    DATE NULL,
        [LastReviewedDate] DATETIME2 NULL,
        [CreatedByUserId]  INT NOT NULL,
        [CreatedDate]      DATETIME2 NOT NULL CONSTRAINT [DF_WikiPage_CreatedDate] DEFAULT (SYSUTCDATETIME()),
        [ModifiedByUserId] INT NOT NULL,
        [ModifiedDate]     DATETIME2 NOT NULL CONSTRAINT [DF_WikiPage_ModifiedDate] DEFAULT (SYSUTCDATETIME()),
        [IsDeleted]        BIT NOT NULL CONSTRAINT [DF_WikiPage_IsDeleted] DEFAULT (0),
        [DeletedDate]      DATETIME2 NULL,
        CONSTRAINT [FK_WikiPage_Company] FOREIGN KEY ([CompanyId]) REFERENCES [dbo].[Company] ([Id]),
        CONSTRAINT [FK_WikiPage_Section] FOREIGN KEY ([SectionId]) REFERENCES [dbo].[WikiSection] ([Id]),
        CONSTRAINT [FK_WikiPage_Parent] FOREIGN KEY ([ParentId]) REFERENCES [dbo].[WikiPage] ([Id]),
        CONSTRAINT [FK_WikiPage_Owner] FOREIGN KEY ([OwnerUserId]) REFERENCES [dbo].[Users] ([Id]),
        CONSTRAINT [FK_WikiPage_CreatedByUser] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[Users] ([Id]),
        CONSTRAINT [FK_WikiPage_ModifiedByUser] FOREIGN KEY ([ModifiedByUserId]) REFERENCES [dbo].[Users] ([Id])
    );
    CREATE INDEX [IX_WikiPage_Section_Parent_Sort] ON [dbo].[WikiPage]([SectionId], [ParentId], [SortOrder]);
    CREATE INDEX [IX_WikiPage_CompanyId] ON [dbo].[WikiPage]([CompanyId]);
END"),

            ("WikiPageVersion", @"
IF OBJECT_ID(N'[dbo].[WikiPageVersion]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WikiPageVersion]
    (
        [Id]            INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_WikiPageVersion] PRIMARY KEY,
        [PageId]        INT NOT NULL,
        [VersionNumber] INT NOT NULL,
        [Title]         NVARCHAR(300) NOT NULL,
        [ContentJson]   NVARCHAR(MAX) NULL,
        [ContentHtml]   NVARCHAR(MAX) NULL,
        [ContentText]   NVARCHAR(MAX) NULL,
        [Comment]       NVARCHAR(300) NULL,
        [AuthorUserId]  INT NOT NULL,
        [CreatedDate]   DATETIME2 NOT NULL CONSTRAINT [DF_WikiPageVersion_CreatedDate] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [FK_WikiPageVersion_Page] FOREIGN KEY ([PageId]) REFERENCES [dbo].[WikiPage] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WikiPageVersion_Author] FOREIGN KEY ([AuthorUserId]) REFERENCES [dbo].[Users] ([Id])
    );
    CREATE UNIQUE INDEX [UQ_WikiPageVersion_Page_Number] ON [dbo].[WikiPageVersion]([PageId], [VersionNumber]);
END"),

            ("WikiFavorite", @"
IF OBJECT_ID(N'[dbo].[WikiFavorite]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WikiFavorite]
    (
        [Id]          INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_WikiFavorite] PRIMARY KEY,
        [PageId]      INT NOT NULL,
        [UserId]      INT NOT NULL,
        [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_WikiFavorite_CreatedDate] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [FK_WikiFavorite_Page] FOREIGN KEY ([PageId]) REFERENCES [dbo].[WikiPage] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WikiFavorite_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id])
    );
    CREATE UNIQUE INDEX [UQ_WikiFavorite_User_Page] ON [dbo].[WikiFavorite]([UserId], [PageId]);
END"),

            ("WikiAttachment", @"
IF OBJECT_ID(N'[dbo].[WikiAttachment]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WikiAttachment]
    (
        [Id]              INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_WikiAttachment] PRIMARY KEY,
        [CompanyId]       INT NOT NULL,
        [PageId]          INT NOT NULL,
        [IsInline]        BIT NOT NULL CONSTRAINT [DF_WikiAttachment_IsInline] DEFAULT (0),
        [CreatedByUserId] INT NOT NULL,
        [CreatedDate]     DATETIME2 NOT NULL CONSTRAINT [DF_WikiAttachment_CreatedDate] DEFAULT (SYSUTCDATETIME()),
        [IsDeleted]       BIT NOT NULL CONSTRAINT [DF_WikiAttachment_IsDeleted] DEFAULT (0),
        CONSTRAINT [FK_WikiAttachment_Company] FOREIGN KEY ([CompanyId]) REFERENCES [dbo].[Company] ([Id]),
        CONSTRAINT [FK_WikiAttachment_Page] FOREIGN KEY ([PageId]) REFERENCES [dbo].[WikiPage] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WikiAttachment_CreatedByUser] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[Users] ([Id])
    );
    CREATE INDEX [IX_WikiAttachment_PageId] ON [dbo].[WikiAttachment]([PageId]);
END"),

            ("WikiAttachmentVersion", @"
IF OBJECT_ID(N'[dbo].[WikiAttachmentVersion]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WikiAttachmentVersion]
    (
        [Id]               INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_WikiAttachmentVersion] PRIMARY KEY,
        [AttachmentId]     INT NOT NULL,
        [VersionNumber]    INT NOT NULL,
        [FileName]         NVARCHAR(255) NOT NULL,
        [StoredName]       NVARCHAR(100) NOT NULL,
        [ContentType]      NVARCHAR(150) NOT NULL,
        [SizeBytes]        BIGINT NOT NULL,
        [UploadedByUserId] INT NOT NULL,
        [UploadedDate]     DATETIME2 NOT NULL CONSTRAINT [DF_WikiAttachmentVersion_UploadedDate] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [FK_WikiAttachmentVersion_Attachment] FOREIGN KEY ([AttachmentId]) REFERENCES [dbo].[WikiAttachment] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WikiAttachmentVersion_User] FOREIGN KEY ([UploadedByUserId]) REFERENCES [dbo].[Users] ([Id])
    );
    CREATE UNIQUE INDEX [UQ_WikiAttachmentVersion_Attachment_Number] ON [dbo].[WikiAttachmentVersion]([AttachmentId], [VersionNumber]);
END"),

            ("WikiPageTask", @"
IF OBJECT_ID(N'[dbo].[WikiPageTask]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WikiPageTask]
    (
        [Id]              INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_WikiPageTask] PRIMARY KEY,
        [PageId]          INT NOT NULL,
        [TaskId]          INT NOT NULL,
        [CreatedByUserId] INT NOT NULL,
        [CreatedDate]     DATETIME2 NOT NULL CONSTRAINT [DF_WikiPageTask_CreatedDate] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [FK_WikiPageTask_Page] FOREIGN KEY ([PageId]) REFERENCES [dbo].[WikiPage] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WikiPageTask_Task] FOREIGN KEY ([TaskId]) REFERENCES [dbo].[TaskCrm] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WikiPageTask_User] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[Users] ([Id])
    );
    CREATE UNIQUE INDEX [UQ_WikiPageTask_Page_Task] ON [dbo].[WikiPageTask]([PageId], [TaskId]);
    CREATE INDEX [IX_WikiPageTask_TaskId] ON [dbo].[WikiPageTask]([TaskId]);
END"),
        };
    }
}
