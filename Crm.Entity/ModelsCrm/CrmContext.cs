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

    public virtual DbSet<Company> Companies { get; set; }

    public virtual DbSet<CompanyUser> CompanyUsers { get; set; }

    public virtual DbSet<Deal> Deals { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    public virtual DbSet<Invitation> Invitations { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<Component> Components { get; set; }

    public virtual DbSet<ProductTemplate> ProductTemplates { get; set; }

    public virtual DbSet<ProductTemplateComponent> ProductTemplateComponents { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<ProductionTask> ProductionTasks { get; set; }

    public virtual DbSet<Contractor> Contractors { get; set; }

    public virtual DbSet<ContractorAccessToken> ContractorAccessTokens { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectUser> ProjectUsers { get; set; }

    public virtual DbSet<RefStatus> RefStatuses { get; set; }

    public virtual DbSet<RememberedDevice> RememberedDevices { get; set; }

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
            entity.HasIndex(e => e.CompanyId);

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Clients_Company");
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

        modelBuilder.Entity<Deal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Deal");
            entity.Property(e => e.Title).HasMaxLength(255).IsRequired();
            entity.Property(e => e.ClientName).HasMaxLength(255);
            entity.Property(e => e.ClientId);
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("Новая");
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.InstallationAddress).HasMaxLength(500);
            entity.Property(e => e.PrepaymentAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PublicToken).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("(getutcdate())");
            entity.HasIndex(e => new { e.CompanyId, e.Status, e.ModifiedDate });
            entity.HasIndex(e => e.PublicToken).IsUnique();

            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(e => e.Client)
                .WithMany(c => c.Deals)
                .HasForeignKey(e => e.ClientId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Owner)
                .WithMany()
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull);
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

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId);
            entity.ToTable("Supplier");

            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ContactPerson).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasIndex(e => e.CompanyId);

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Component>(entity =>
        {
            entity.HasKey(e => e.ComponentId);
            entity.ToTable("Component");

            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Unit).HasMaxLength(20).IsRequired();
            entity.Property(e => e.CostPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.StockQuantity).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.ReorderLevel).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasIndex(e => e.CompanyId);

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Supplier).WithMany(p => p.Components)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ProductTemplate>(entity =>
        {
            entity.HasKey(e => e.ProductTemplateId);
            entity.ToTable("ProductTemplate");

            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Unit).HasMaxLength(20).IsRequired();
            entity.Property(e => e.FormulaExpression).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.DefaultMarginPercent).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasIndex(e => e.CompanyId);

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ProductTemplateComponent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ProductTemplateComponent");

            entity.Property(e => e.QuantityFormula).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(200);

            entity.HasOne(d => d.ProductTemplate).WithMany(p => p.ProductTemplateComponents)
                .HasForeignKey(d => d.ProductTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Component).WithMany(p => p.ProductTemplateComponents)
                .HasForeignKey(d => d.ComponentId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId);
            entity.ToTable("OrderItem");

            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 3)").HasDefaultValue(1m);
            entity.Property(e => e.Width).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.Height).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.Depth).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.CostPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MarginPercent).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("Новое");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getutcdate())");
            entity.HasIndex(e => e.DealId);

            entity.HasOne(d => d.Deal).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.DealId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.ProductTemplate).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductTemplateId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ProductionTask>(entity =>
        {
            entity.HasKey(e => e.ProductionTaskId);
            entity.ToTable("ProductionTask");

            entity.Property(e => e.StageName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ExecutorType).HasMaxLength(20).HasDefaultValue("Internal");
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Pending");
            entity.Property(e => e.PhotoUrl).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasIndex(e => new { e.OrderItemId, e.StageOrder });

            entity.HasOne(d => d.OrderItem).WithMany(p => p.ProductionTasks)
                .HasForeignKey(d => d.OrderItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.AssignedUser)
                .WithMany()
                .HasForeignKey(d => d.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Contractor).WithMany(p => p.ProductionTasks)
                .HasForeignKey(d => d.ContractorId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Contractor>(entity =>
        {
            entity.HasKey(e => e.ContractorId);
            entity.ToTable("Contractor");

            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasIndex(e => e.CompanyId);

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ContractorAccessToken>(entity =>
        {
            entity.HasKey(e => e.ContractorAccessTokenId);
            entity.ToTable("ContractorAccessToken");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.HasIndex(e => e.Token).IsUnique();

            entity.HasOne(d => d.Contractor)
                .WithMany()
                .HasForeignKey(d => d.ContractorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.ProductionTask).WithMany(p => p.ContractorAccessTokens)
                .HasForeignKey(d => d.ProductionTaskId)
                .OnDelete(DeleteBehavior.Cascade);
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
