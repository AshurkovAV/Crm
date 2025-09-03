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

    public virtual DbSet<CustomerOrder> CustomerOrders { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Material> Materials { get; set; }

    public virtual DbSet<MaterialPurchase> MaterialPurchases { get; set; }

    public virtual DbSet<MaterialUsage> MaterialUsages { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductCategory> ProductCategories { get; set; }

    public virtual DbSet<ProductSpecification> ProductSpecifications { get; set; }

    public virtual DbSet<ProductionOrder> ProductionOrders { get; set; }

    public virtual DbSet<PurchaseDetail> PurchaseDetails { get; set; }

    public virtual DbSet<Shipment> Shipments { get; set; }

    public virtual DbSet<ShipmentDetail> ShipmentDetails { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserToken> UserTokens { get; set; }

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

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC0779243ABA");

            entity.ToTable(tb => tb.HasTrigger("trg_Users_UpdateModifiedDate"));

            entity.Property(e => e.ClientId).HasMaxLength(255);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DefaultAvatarId).HasMaxLength(255);
            entity.Property(e => e.DefaultEmail).HasMaxLength(255);
            entity.Property(e => e.DefaultPhone).HasMaxLength(50);
            entity.Property(e => e.DeviceId).HasMaxLength(255);
            entity.Property(e => e.DisplayName).HasMaxLength(255);
            entity.Property(e => e.FirstName).HasMaxLength(255);
            entity.Property(e => e.IdYandex).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsAvatarEmpty).HasMaxLength(10);
            entity.Property(e => e.LastName).HasMaxLength(255);
            entity.Property(e => e.Login).HasMaxLength(255);
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.RealName).HasMaxLength(255);
            entity.Property(e => e.Role).HasMaxLength(20);
            entity.Property(e => e.Sex).HasMaxLength(10);
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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
