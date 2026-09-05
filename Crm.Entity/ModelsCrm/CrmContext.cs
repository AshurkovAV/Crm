using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Crm.Entity.ModelsCrm;

public partial class CrmContext : DbContext
{
    public CrmContext()
    {
    }

    public CrmContext(DbContextOptions<CrmContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<ClientInteraction> ClientInteractions { get; set; }

    public virtual DbSet<Company> Companies { get; set; }

    public virtual DbSet<CompanyUser> CompanyUsers { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    public virtual DbSet<CustomerOrder> CustomerOrders { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Invitation> Invitations { get; set; }

    public virtual DbSet<Material> Materials { get; set; }

    public virtual DbSet<MaterialPurchase> MaterialPurchases { get; set; }

    public virtual DbSet<MaterialUsage> MaterialUsages { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductCategory> ProductCategories { get; set; }

    public virtual DbSet<ProductSpecification> ProductSpecifications { get; set; }

    public virtual DbSet<ProductionOrder> ProductionOrders { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectUser> ProjectUsers { get; set; }

    public virtual DbSet<PurchaseDetail> PurchaseDetails { get; set; }

    public virtual DbSet<RefStatus> RefStatuses { get; set; }

    public virtual DbSet<RememberedDevice> RememberedDevices { get; set; }

    public virtual DbSet<Shipment> Shipments { get; set; }

    public virtual DbSet<ShipmentDetail> ShipmentDetails { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<TaskCrm> TaskCrms { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserToken> UserTokens { get; set; }

    public virtual DbSet<VerificationToken> VerificationTokens { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=OMSIT;Database=crm;User ID=sa;Password=12345678;encrypt=false");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.ClientId).HasName("PK__Clients__E67E1A04C30CB090");

            entity.Property(e => e.ClientId).HasColumnName("ClientID");
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.ContactPerson).HasMaxLength(100);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.TaxNumber).HasMaxLength(20);
        });

        modelBuilder.Entity<ClientInteraction>(entity =>
        {
            entity.HasKey(e => e.InteractionId).HasName("PK__ClientIn__922C037686D1BB13");

            entity.Property(e => e.InteractionId).HasColumnName("InteractionID");
            entity.Property(e => e.ClientId).HasColumnName("ClientID");
            entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");
            entity.Property(e => e.FollowUpDate).HasColumnType("datetime");
            entity.Property(e => e.FollowUpDone).HasDefaultValue(false);
            entity.Property(e => e.InteractionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.InteractionType).HasMaxLength(50);

            entity.HasOne(d => d.Client).WithMany(p => p.ClientInteractions)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Interactions_Clients");

            entity.HasOne(d => d.Employee).WithMany(p => p.ClientInteractions)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK_Interactions_Employees");
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Company__3214EC07AA7E0F81");

            entity.ToTable("Company");

            entity.Property(e => e.InviteCode).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MaxProjects).HasDefaultValue(10);
            entity.Property(e => e.Name).HasMaxLength(255);

            entity.HasOne(d => d.Owner).WithMany(p => p.Companies)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Company_User");
        });

        modelBuilder.Entity<CompanyUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CompanyU__3214EC07ED2B4E6D");

            entity.ToTable("CompanyUser");

            entity.HasIndex(e => new { e.CompanyId, e.UserId, e.IsActive }, "UK_CompanyUser").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.JoinedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Position).HasMaxLength(255);
            entity.Property(e => e.Role).HasMaxLength(50);

            entity.HasOne(d => d.Company).WithMany(p => p.CompanyUsers)
                .HasForeignKey(d => d.CompanyId)
                .HasConstraintName("FK_CompanyUser_Company");

            entity.HasOne(d => d.User).WithMany(p => p.CompanyUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CompanyUser_User");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.ToTable("ChatMessage");

            entity.Property(e => e.Text)
                .HasMaxLength(4000)
                .IsRequired();
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false);

            entity.HasIndex(e => new { e.CompanyId, e.SenderUserId, e.RecipientUserId, e.SentAt });

            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(e => e.SenderUser)
                .WithMany()
                .HasForeignKey(e => e.SenderUserId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(e => e.RecipientUser)
                .WithMany()
                .HasForeignKey(e => e.RecipientUserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<CustomerOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Customer__C3905BAFC640B9EE");

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.ClientId).HasColumnName("ClientID");
            entity.Property(e => e.CompletedDate).HasColumnType("datetime");
            entity.Property(e => e.Discount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.RequiredDate).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("New");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Client).WithMany(p => p.CustomerOrders)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Clients");

            entity.HasOne(d => d.Employee).WithMany(p => p.CustomerOrders)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK_Orders_Employees");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__Employee__7AD04FF156131701");

            entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");
            entity.Property(e => e.Department).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Position).HasMaxLength(50);
        });

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.HasIndex(e => e.Code, "UQ_Invitations_Code").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(16);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.InvitationType)
                .HasMaxLength(20)
                .HasDefaultValue("Email");
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
        });

        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasKey(e => e.MaterialId).HasName("PK__Material__C506131769B1167D");

            entity.Property(e => e.MaterialId).HasColumnName("MaterialID");
            entity.Property(e => e.CurrentStock).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.ReorderLevel).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.StandardCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.UnitOfMeasure).HasMaxLength(20);
        });

        modelBuilder.Entity<MaterialPurchase>(entity =>
        {
            entity.HasKey(e => e.PurchaseId).HasName("PK__Material__6B0A6BDE22C46C5E");

            entity.Property(e => e.PurchaseId).HasColumnName("PurchaseID");
            entity.Property(e => e.DeliveryDate).HasColumnType("datetime");
            entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Ordered");
            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Employee).WithMany(p => p.MaterialPurchases)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK_Purchases_Employees");

            entity.HasOne(d => d.Supplier).WithMany(p => p.MaterialPurchases)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Purchases_Suppliers");
        });

        modelBuilder.Entity<MaterialUsage>(entity =>
        {
            entity.HasKey(e => e.UsageId).HasName("PK__Material__29B197C0E00BE638");

            entity.ToTable("MaterialUsage");

            entity.Property(e => e.UsageId).HasColumnName("UsageID");
            entity.Property(e => e.MaterialId).HasColumnName("MaterialID");
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.ProductionOrderId).HasColumnName("ProductionOrderID");
            entity.Property(e => e.QuantityUsed).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.UsageDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Material).WithMany(p => p.MaterialUsages)
                .HasForeignKey(d => d.MaterialId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MaterialUsage_Materials");

            entity.HasOne(d => d.ProductionOrder).WithMany(p => p.MaterialUsages)
                .HasForeignKey(d => d.ProductionOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MaterialUsage_ProductionOrders");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.OrderDetailId).HasName("PK__OrderDet__D3B9D30C28F880EA");

            entity.Property(e => e.OrderDetailId).HasColumnName("OrderDetailID");
            entity.Property(e => e.Discount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("New");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderDetails_Orders");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderDetails_Products");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A58B487C309");

            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_Payments_Orders");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6ED67C8AD14");

            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.SellingPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.StandardCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitOfMeasure).HasMaxLength(20);

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Products__Catego__2D27B809");
        });

        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__ProductC__19093A2BAE56C130");

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<ProductSpecification>(entity =>
        {
            entity.HasKey(e => e.SpecificationId).HasName("PK__ProductS__A384CC1D63358D9D");

            entity.Property(e => e.SpecificationId).HasColumnName("SpecificationID");
            entity.Property(e => e.MaterialId).HasColumnName("MaterialID");
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 3)");

            entity.HasOne(d => d.Material).WithMany(p => p.ProductSpecifications)
                .HasForeignKey(d => d.MaterialId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Specifications_Materials");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductSpecifications)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Specifications_Products");
        });

        modelBuilder.Entity<ProductionOrder>(entity =>
        {
            entity.HasKey(e => e.ProductionOrderId).HasName("PK__Producti__E86155F0F608112C");

            entity.Property(e => e.ProductionOrderId).HasColumnName("ProductionOrderID");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.OrderDetailId).HasColumnName("OrderDetailID");
            entity.Property(e => e.ResponsibleEmployeeId).HasColumnName("ResponsibleEmployeeID");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Planned");

            entity.HasOne(d => d.OrderDetail).WithMany(p => p.ProductionOrders)
                .HasForeignKey(d => d.OrderDetailId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductionOrders_OrderDetails");

            entity.HasOne(d => d.ResponsibleEmployee).WithMany(p => p.ProductionOrders)
                .HasForeignKey(d => d.ResponsibleEmployeeId)
                .HasConstraintName("FK_ProductionOrders_Employees");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Project__3214EC075C83E530");

            entity.ToTable("Project");

            entity.Property(e => e.Activity).HasMaxLength(255);
            entity.Property(e => e.AttachmentType).HasMaxLength(255);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Efficiency).HasMaxLength(255);
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Role).HasMaxLength(255);
            entity.Property(e => e.Status).HasMaxLength(100);
        });

        modelBuilder.Entity<ProjectUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ProjectU__3214EC07B8AA4E1E");

            entity.ToTable("ProjectUser");

            entity.HasIndex(e => new { e.ProjectId, e.UserId }, "IX_ProjectUser_ProjectId_UserId").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.JoinedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Role).HasMaxLength(100);

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectUsers)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_ProjectUser_Project");

            entity.HasOne(d => d.User).WithMany(p => p.ProjectUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProjectUser_User");
        });

        modelBuilder.Entity<PurchaseDetail>(entity =>
        {
            entity.HasKey(e => e.PurchaseDetailId).HasName("PK__Purchase__88C328D57E3E9643");

            entity.Property(e => e.PurchaseDetailId).HasColumnName("PurchaseDetailID");
            entity.Property(e => e.MaterialId).HasColumnName("MaterialID");
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.PurchaseId).HasColumnName("PurchaseID");
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.ReceivedQuantity)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 3)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Material).WithMany(p => p.PurchaseDetails)
                .HasForeignKey(d => d.MaterialId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PurchaseDetails_Materials");

            entity.HasOne(d => d.Purchase).WithMany(p => p.PurchaseDetails)
                .HasForeignKey(d => d.PurchaseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PurchaseDetails_Purchases");
        });

        modelBuilder.Entity<RefStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_Stat__3214EC076DFB1AB0");

            entity.ToTable("Ref_Statuses");

            entity.HasIndex(e => e.Name, "UQ__Ref_Stat__737584F684CC72A7").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
        });

        modelBuilder.Entity<RememberedDevice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Remember__3214EC07A4311EC2");

            entity.ToTable("RememberedDevice");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.DeviceId).HasMaxLength(255);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.RememberToken).HasMaxLength(255);

            entity.HasOne(d => d.User).WithMany(p => p.RememberedDevices)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RememberedDevice_Users");
        });

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasKey(e => e.ShipmentId).HasName("PK__Shipment__5CAD378DB04D1D66");

            entity.Property(e => e.ShipmentId).HasColumnName("ShipmentID");
            entity.Property(e => e.Carrier).HasMaxLength(100);
            entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.ShipmentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TrackingNumber).HasMaxLength(50);

            entity.HasOne(d => d.Employee).WithMany(p => p.Shipments)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK_Shipments_Employees");

            entity.HasOne(d => d.Order).WithMany(p => p.Shipments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Shipments_Orders");
        });

        modelBuilder.Entity<ShipmentDetail>(entity =>
        {
            entity.HasKey(e => e.ShipmentDetailId).HasName("PK__Shipment__047142C0B5882E9A");

            entity.Property(e => e.ShipmentDetailId).HasColumnName("ShipmentDetailID");
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.OrderDetailId).HasColumnName("OrderDetailID");
            entity.Property(e => e.QuantityShipped).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.ShipmentId).HasColumnName("ShipmentID");

            entity.HasOne(d => d.OrderDetail).WithMany(p => p.ShipmentDetails)
                .HasForeignKey(d => d.OrderDetailId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShipmentDetails_OrderDetails");

            entity.HasOne(d => d.Shipment).WithMany(p => p.ShipmentDetails)
                .HasForeignKey(d => d.ShipmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShipmentDetails_Shipments");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId).HasName("PK__Supplier__4BE666941AC300F7");

            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.ContactPerson).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.TaxNumber).HasMaxLength(20);
        });

        modelBuilder.Entity<TaskCrm>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TaskCrm__3214EC07698711A5");

            entity.ToTable("TaskCrm", tb => tb.HasComment("Таблица задач проекта. Содержит все задачи, назначенные на сотрудников, с указанием сроков и статусов."));

            entity.Property(e => e.Id).HasComment("Уникальный идентификатор задачи. Автоинкрементное поле (IDENTITY). Первичный ключ таблицы.");
            entity.Property(e => e.Activity).HasComment("Дата и время последней активности по задаче. Обновляется при любом изменении: комментарий, смена статуса, редактирование. Формат: YYYY-MM-DD HH:MI:SS");
            entity.Property(e => e.Assignee).HasComment("Исполнитель задачи. ФИО сотрудника, ответственного за выполнение. Отображается в столбце \"Исполнитель\" интерфейса.");
            entity.Property(e => e.Author).HasComment("Постановщик задачи. ФИО сотрудника, создавшего задачу. Отображается в столбце \"Постановщик\" интерфейса.");
            entity.Property(e => e.Comments).HasComment("Комментарии к задаче. Текстовое поле неограниченной длины (MAX). Хранит обсуждения, уточнения и историю выполнения задачи.");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasComment("Дата и время создания задачи. Автоматически устанавливается при вставке записи. Значение по умолчанию - GETDATE().");
            entity.Property(e => e.Deadline).HasComment("Крайний срок выполнения задачи. Используется для контроля просрочек и планирования. При превышении текущей даты задача считается просроченной.");
            entity.Property(e => e.IsOverdue)
                .HasDefaultValue(false)
                .HasComment("Флаг просрочки задачи. Вычисляемое поле: TRUE, если Deadline < текущей даты и статус не \"Завершена\". Используется для быстрой фильтрации просроченных задач.");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("(getdate())")
                .HasComment("Дата и время последнего изменения задачи. Автоматически обновляется при изменении записи. Значение по умолчанию - GETDATE().");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasComment("Название задачи. Обязательное поле. Отображается в списке задач как основное описание.");
            entity.Property(e => e.Priority)
                .HasMaxLength(50)
                .HasComment("Приоритет задачи. Возможные значения: \"Высокий\", \"Средний\", \"Низкий\". Используется для сортировки задач по важности.");
            entity.Property(e => e.ProjectId).HasComment("Внешний ключ на таблицу Project. Определяет принадлежность задачи к конкретному проекту. Может быть NULL, если задача не привязана к проекту. При удалении проекта значение становится NULL (ON DELETE SET NULL).");
            entity.Property(e => e.Status)
                .HasDefaultValue(1)
                .HasComment("Статус выполнения задачи. Возможные значения: \"Новая\", \"В работе\", \"Просрочена\", \"Завершена\", \"Отменена\". По умолчанию - \"Новая\".");
            entity.Property(e => e.Tags)
                .HasMaxLength(500)
                .HasComment("Теги задачи. Хранятся в виде строки с разделителями (например: \"срочно,важно,клиент\"). Используются для категоризации и фильтрации задач. Максимальная длина - 500 символов.");

            entity.HasOne(d => d.Project).WithMany(p => p.TaskCrms)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Task_Project");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC071D51B417");

            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.AlternativeEmail)
                .HasMaxLength(255)
                .HasComment("Альтернативный email");
            entity.Property(e => e.AlternativePhone).HasMaxLength(50);
            entity.Property(e => e.Bio).HasMaxLength(1000);
            entity.Property(e => e.BirthPlace)
                .HasMaxLength(500)
                .HasComment("Место рождения");
            entity.Property(e => e.ClientId).HasMaxLength(255);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DefaultAvatarId).HasMaxLength(255);
            entity.Property(e => e.DefaultEmail).HasMaxLength(255);
            entity.Property(e => e.DefaultPhone).HasMaxLength(50);
            entity.Property(e => e.Department).HasMaxLength(255);
            entity.Property(e => e.DeviceId).HasMaxLength(255);
            entity.Property(e => e.DisplayName).HasMaxLength(255);
            entity.Property(e => e.FirstName).HasMaxLength(255);
            entity.Property(e => e.IdYandex).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsAvatarEmpty).HasMaxLength(10);
            entity.Property(e => e.LastName).HasMaxLength(255);
            entity.Property(e => e.LinkedIn).HasMaxLength(255);
            entity.Property(e => e.Login).HasMaxLength(255);
            entity.Property(e => e.MiddleName)
                .HasMaxLength(255)
                .HasComment("Отчество пользователя");
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PasswordSalt).HasMaxLength(255);
            entity.Property(e => e.Position)
                .HasMaxLength(255)
                .HasComment("Должность");
            entity.Property(e => e.RealName).HasMaxLength(255);
            entity.Property(e => e.Role).HasMaxLength(20);
            entity.Property(e => e.Sex).HasMaxLength(10);
            entity.Property(e => e.Telegram)
                .HasMaxLength(100)
                .HasComment("Telegram username");
            entity.Property(e => e.Website).HasMaxLength(255);
            entity.Property(e => e.WhatsApp).HasMaxLength(50);
            entity.Property(e => e.WorkPhone).HasMaxLength(50);
        });

        modelBuilder.Entity<UserToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserToke__3214EC07FD9C6DDE");

            entity.ToTable("UserToken");

            entity.Property(e => e.DateEdit).HasColumnType("datetime");
            entity.Property(e => e.DeviceId).HasMaxLength(100);
            entity.Property(e => e.Scope).HasMaxLength(500);
            entity.Property(e => e.TokenType).HasMaxLength(100);
        });

        modelBuilder.Entity<VerificationToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Verifica__3214EC077CC85B91");

            entity.ToTable("VerificationToken");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Token).HasMaxLength(255);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
