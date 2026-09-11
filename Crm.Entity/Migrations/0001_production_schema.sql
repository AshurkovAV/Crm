/*
    Миграция схемы под ТЗ "BigLV: Прозрачный завод".

    ВНИМАНИЕ, ПРИМЕНЯЕТСЯ ВРУЧНУЮ:
    - Это НЕ EF Core Migration (проект использует database-first CrmContext),
      применить нужно самостоятельно через SSMS/sqlcmd к базе `crm` на сервере OMSIT.
    - Перед запуском СДЕЛАЙТЕ РЕЗЕРВНУЮ КОПИЮ БАЗЫ (BACKUP DATABASE).
    - Блок 1 удаляет старый неиспользуемый "демо"-набор таблиц (Product/Material/
      CustomerOrder/ProductionOrder/...), на которые в коде нет ни одной ссылки
      (проверено: нет контроллеров, нет репозиториев). Если в них есть ценные
      данные — выгрузите их до запуска, скрипт восстановить их не даст.
    - Блок 2 добавляет CompanyId в Clients (мультитенантность) и новые поля в Deal.
      Существующие строки Clients получат CompanyId = NULL — их нужно вручную
      разнести по компаниям (или проставить компанию по умолчанию) ДО того,
      как делать колонку NOT NULL (последний шаг блока 2 закомментирован).
    - Блок 3 создаёт новые таблицы: Supplier, Component, ProductTemplate,
      ProductTemplateComponent, OrderItem, ProductionTask, Contractor,
      ContractorAccessToken.
*/

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-------------------------------------------------------------------------------
-- Блок 1. Удаление старого неиспользуемого набора таблиц (демо-схема склада)
-------------------------------------------------------------------------------

IF OBJECT_ID('dbo.ShipmentDetails', 'U') IS NOT NULL DROP TABLE dbo.ShipmentDetails;
IF OBJECT_ID('dbo.PurchaseDetails', 'U') IS NOT NULL DROP TABLE dbo.PurchaseDetails;
IF OBJECT_ID('dbo.MaterialUsage', 'U') IS NOT NULL DROP TABLE dbo.MaterialUsage;
IF OBJECT_ID('dbo.ProductionOrders', 'U') IS NOT NULL DROP TABLE dbo.ProductionOrders;
IF OBJECT_ID('dbo.Shipments', 'U') IS NOT NULL DROP TABLE dbo.Shipments;
IF OBJECT_ID('dbo.Payments', 'U') IS NOT NULL DROP TABLE dbo.Payments;
IF OBJECT_ID('dbo.OrderDetails', 'U') IS NOT NULL DROP TABLE dbo.OrderDetails;
IF OBJECT_ID('dbo.ProductSpecifications', 'U') IS NOT NULL DROP TABLE dbo.ProductSpecifications;
IF OBJECT_ID('dbo.MaterialPurchases', 'U') IS NOT NULL DROP TABLE dbo.MaterialPurchases;
IF OBJECT_ID('dbo.CustomerOrders', 'U') IS NOT NULL DROP TABLE dbo.CustomerOrders;
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.ProductCategories', 'U') IS NOT NULL DROP TABLE dbo.ProductCategories;
IF OBJECT_ID('dbo.Materials', 'U') IS NOT NULL DROP TABLE dbo.Materials;
IF OBJECT_ID('dbo.ClientInteractions', 'U') IS NOT NULL DROP TABLE dbo.ClientInteractions;
IF OBJECT_ID('dbo.Employees', 'U') IS NOT NULL DROP TABLE dbo.Employees;
IF OBJECT_ID('dbo.Suppliers', 'U') IS NOT NULL DROP TABLE dbo.Suppliers;

-------------------------------------------------------------------------------
-- Блок 2. Мультитенантность Clients + новые поля Deal
-------------------------------------------------------------------------------

IF COL_LENGTH('dbo.Clients', 'CompanyId') IS NULL
BEGIN
    ALTER TABLE dbo.Clients ADD CompanyId INT NULL;
END;

-- ЗАПОЛНИТЕ CompanyId для существующих клиентов вручную, затем раскомментируйте:
-- ALTER TABLE dbo.Clients ALTER COLUMN CompanyId INT NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Clients_Company')
BEGIN
    ALTER TABLE dbo.Clients ADD CONSTRAINT FK_Clients_Company
        FOREIGN KEY (CompanyId) REFERENCES dbo.Company(Id);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Clients_CompanyId')
BEGIN
    CREATE INDEX IX_Clients_CompanyId ON dbo.Clients(CompanyId);
END;

IF COL_LENGTH('dbo.Deal', 'InstallationAddress') IS NULL
BEGIN
    ALTER TABLE dbo.Deal ADD InstallationAddress NVARCHAR(500) NULL;
END;

IF COL_LENGTH('dbo.Deal', 'PrepaymentAmount') IS NULL
BEGIN
    ALTER TABLE dbo.Deal ADD PrepaymentAmount DECIMAL(18, 2) NULL;
END;

IF COL_LENGTH('dbo.Deal', 'PublicToken') IS NULL
BEGIN
    ALTER TABLE dbo.Deal ADD PublicToken UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID();
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Deal_PublicToken')
BEGIN
    CREATE UNIQUE INDEX UQ_Deal_PublicToken ON dbo.Deal(PublicToken);
END;

-------------------------------------------------------------------------------
-- Блок 3. Новые таблицы производственного модуля
-------------------------------------------------------------------------------

CREATE TABLE dbo.Supplier
(
    SupplierId    INT IDENTITY(1,1) PRIMARY KEY,
    CompanyId     INT NOT NULL REFERENCES dbo.Company(Id),
    Name          NVARCHAR(100) NOT NULL,
    ContactPerson NVARCHAR(100) NULL,
    Phone         NVARCHAR(20) NULL,
    Email         NVARCHAR(100) NULL,
    Notes         NVARCHAR(500) NULL,
    IsActive      BIT NOT NULL DEFAULT 1
);
CREATE INDEX IX_Supplier_CompanyId ON dbo.Supplier(CompanyId);

CREATE TABLE dbo.Component
(
    ComponentId   INT IDENTITY(1,1) PRIMARY KEY,
    CompanyId     INT NOT NULL REFERENCES dbo.Company(Id),
    Name          NVARCHAR(200) NOT NULL,
    Unit          NVARCHAR(20) NOT NULL,
    CostPrice     DECIMAL(18, 2) NOT NULL,
    StockQuantity DECIMAL(18, 3) NOT NULL DEFAULT 0,
    ReorderLevel  DECIMAL(18, 3) NULL,
    SupplierId    INT NULL REFERENCES dbo.Supplier(SupplierId),
    IsActive      BIT NOT NULL DEFAULT 1
);
CREATE INDEX IX_Component_CompanyId ON dbo.Component(CompanyId);

CREATE TABLE dbo.ProductTemplate
(
    ProductTemplateId   INT IDENTITY(1,1) PRIMARY KEY,
    CompanyId           INT NOT NULL REFERENCES dbo.Company(Id),
    Name                NVARCHAR(200) NOT NULL,
    Category            NVARCHAR(100) NULL,
    Unit                NVARCHAR(20) NOT NULL,
    FormulaExpression   NVARCHAR(2000) NOT NULL,
    DefaultMarginPercent DECIMAL(5, 2) NULL,
    IsActive            BIT NOT NULL DEFAULT 1
);
CREATE INDEX IX_ProductTemplate_CompanyId ON dbo.ProductTemplate(CompanyId);

CREATE TABLE dbo.ProductTemplateComponent
(
    Id                INT IDENTITY(1,1) PRIMARY KEY,
    ProductTemplateId INT NOT NULL REFERENCES dbo.ProductTemplate(ProductTemplateId) ON DELETE CASCADE,
    ComponentId       INT NOT NULL REFERENCES dbo.Component(ComponentId),
    QuantityFormula   NVARCHAR(500) NOT NULL,
    Notes             NVARCHAR(200) NULL
);
CREATE INDEX IX_ProductTemplateComponent_ProductTemplateId ON dbo.ProductTemplateComponent(ProductTemplateId);

CREATE TABLE dbo.OrderItem
(
    OrderItemId       INT IDENTITY(1,1) PRIMARY KEY,
    DealId            INT NOT NULL REFERENCES dbo.Deal(Id) ON DELETE CASCADE,
    ProductTemplateId INT NULL REFERENCES dbo.ProductTemplate(ProductTemplateId),
    Name              NVARCHAR(255) NOT NULL,
    Quantity          DECIMAL(18, 3) NOT NULL DEFAULT 1,
    Width             DECIMAL(18, 3) NULL,
    Height            DECIMAL(18, 3) NULL,
    Depth             DECIMAL(18, 3) NULL,
    CostPrice         DECIMAL(18, 2) NOT NULL DEFAULT 0,
    MarginPercent     DECIMAL(5, 2) NULL,
    Price             DECIMAL(18, 2) NOT NULL DEFAULT 0,
    Status            NVARCHAR(50) NOT NULL DEFAULT N'Новое',
    CreatedDate       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_OrderItem_DealId ON dbo.OrderItem(DealId);

CREATE TABLE dbo.Contractor
(
    ContractorId INT IDENTITY(1,1) PRIMARY KEY,
    CompanyId    INT NOT NULL REFERENCES dbo.Company(Id),
    Name         NVARCHAR(200) NOT NULL,
    Phone        NVARCHAR(20) NULL,
    Email        NVARCHAR(100) NULL,
    Notes        NVARCHAR(500) NULL,
    IsActive     BIT NOT NULL DEFAULT 1
);
CREATE INDEX IX_Contractor_CompanyId ON dbo.Contractor(CompanyId);

CREATE TABLE dbo.ProductionTask
(
    ProductionTaskId INT IDENTITY(1,1) PRIMARY KEY,
    OrderItemId      INT NOT NULL REFERENCES dbo.OrderItem(OrderItemId) ON DELETE CASCADE,
    StageName        NVARCHAR(100) NOT NULL,
    StageOrder       INT NOT NULL DEFAULT 0,
    ExecutorType     NVARCHAR(20) NOT NULL DEFAULT N'Internal',
    AssignedUserId   INT NULL REFERENCES dbo.Users(Id),
    ContractorId     INT NULL REFERENCES dbo.Contractor(ContractorId),
    Status           NVARCHAR(20) NOT NULL DEFAULT N'Pending',
    StartedAt        DATETIME2 NULL,
    CompletedAt      DATETIME2 NULL,
    PhotoUrl         NVARCHAR(500) NULL,
    Notes            NVARCHAR(2000) NULL
);
CREATE INDEX IX_ProductionTask_OrderItemId_StageOrder ON dbo.ProductionTask(OrderItemId, StageOrder);

CREATE TABLE dbo.ContractorAccessToken
(
    ContractorAccessTokenId INT IDENTITY(1,1) PRIMARY KEY,
    ContractorId            INT NOT NULL REFERENCES dbo.Contractor(ContractorId) ON DELETE CASCADE,
    ProductionTaskId        INT NOT NULL REFERENCES dbo.ProductionTask(ProductionTaskId),
    Token                   UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    CreatedAt               DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ExpiresAt               DATETIME2 NULL,
    UsedAt                  DATETIME2 NULL
);
CREATE UNIQUE INDEX UQ_ContractorAccessToken_Token ON dbo.ContractorAccessToken(Token);

COMMIT TRANSACTION;
