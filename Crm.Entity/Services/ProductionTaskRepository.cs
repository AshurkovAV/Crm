using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;

namespace Crm.Entity.Services;

public class ProductionTaskRepository : IProductionTaskRepository
{
    /// <summary>
    /// Дефолтная цепочка этапов производства для изделия, Модуль Б ТЗ.
    /// </summary>
    public static readonly (string StageName, int StageOrder)[] DefaultStages =
    {
        ("Резка", 1),
        ("Обработка", 2),
        ("Сборка", 3),
        ("ОТК", 4)
    };

    private static readonly string[] AllowedStatuses = { "Pending", "InProgress", "Done" };

    public async Task<List<ProductionTask>> GetByOrderItemAsync(int orderItemId, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return new List<ProductionTask>();

        return await db.ProductionTasks
            .AsNoTracking()
            .Include(task => task.AssignedUser)
            .Include(task => task.Contractor)
            .Include(task => task.OrderItem)
                .ThenInclude(orderItem => orderItem.Deal)
            .Where(task => task.OrderItemId == orderItemId
                           && task.OrderItem.Deal.CompanyId == companyId.Value)
            .OrderBy(task => task.StageOrder)
            .ToListAsync();
    }

    public async Task<List<ProductionTask>> GetByCompanyKanbanAsync(int userId, string? stageName = null, string? status = null)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return new List<ProductionTask>();

        var query = db.ProductionTasks
            .AsNoTracking()
            .Include(task => task.AssignedUser)
            .Include(task => task.Contractor)
            .Include(task => task.OrderItem)
                .ThenInclude(orderItem => orderItem.Deal)
                    .ThenInclude(deal => deal.Client)
            .Where(task => task.OrderItem.Deal.CompanyId == companyId.Value);

        if (!string.IsNullOrWhiteSpace(stageName))
            query = query.Where(task => task.StageName == stageName);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(task => task.Status == status);

        return await query
            .OrderBy(task => task.StageOrder)
            .ThenBy(task => task.ProductionTaskId)
            .ToListAsync();
    }

    public async Task<List<ProductionTask>?> GenerateDefaultStagesAsync(int orderItemId, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return null;

        var orderItem = await db.OrderItems
            .Include(oi => oi.Deal)
            .FirstOrDefaultAsync(oi => oi.OrderItemId == orderItemId && oi.Deal.CompanyId == companyId.Value);
        if (orderItem == null)
            return null;

        var hasExisting = await db.ProductionTasks.AnyAsync(task => task.OrderItemId == orderItemId);

        if (!hasExisting)
        {
            foreach (var (stageName, stageOrder) in DefaultStages)
            {
                db.ProductionTasks.Add(new ProductionTask
                {
                    OrderItemId = orderItemId,
                    StageName = stageName,
                    StageOrder = stageOrder,
                    ExecutorType = "Internal",
                    Status = "Pending"
                });
            }

            await db.SaveChangesAsync();
        }

        return await db.ProductionTasks
            .AsNoTracking()
            .Include(task => task.AssignedUser)
            .Include(task => task.Contractor)
            .Include(task => task.OrderItem)
                .ThenInclude(oi => oi.Deal)
                    .ThenInclude(deal => deal.Client)
            .Where(task => task.OrderItemId == orderItemId)
            .OrderBy(task => task.StageOrder)
            .ToListAsync();
    }

    public async Task<ProductionTaskStatusResult> UpdateStatusAsync(int taskId, int userId, string newStatus, string? photoUrl)
    {
        if (!AllowedStatuses.Contains(newStatus))
            return ProductionTaskStatusResult.Fail($"Недопустимый статус: {newStatus}.");

        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return ProductionTaskStatusResult.Fail("У пользователя не выбрана текущая компания.");

        var task = await db.ProductionTasks
            .Include(t => t.OrderItem)
                .ThenInclude(oi => oi.Deal)
            .FirstOrDefaultAsync(t => t.ProductionTaskId == taskId && t.OrderItem.Deal.CompanyId == companyId.Value);
        if (task == null)
            return ProductionTaskStatusResult.Fail("Этап не найден.");

        if (!string.IsNullOrWhiteSpace(photoUrl))
            task.PhotoUrl = photoUrl;

        if (newStatus == "Done")
        {
            var isFinalStage = await IsFinalStageAsync(db, task);
            var hasPhoto = !string.IsNullOrWhiteSpace(task.PhotoUrl);
            if (isFinalStage && !hasPhoto)
            {
                return ProductionTaskStatusResult.Fail(
                    "Для завершения этапа ОТК нужно прикрепить фото готового изделия.");
            }

            task.CompletedAt = DateTime.UtcNow;
            if (!task.StartedAt.HasValue)
                task.StartedAt = task.CompletedAt;
        }
        else if (newStatus == "InProgress")
        {
            if (!task.StartedAt.HasValue)
                task.StartedAt = DateTime.UtcNow;
            task.CompletedAt = null;
        }
        else
        {
            // Возврат в Pending — сбрасываем отметки времени.
            task.StartedAt = null;
            task.CompletedAt = null;
        }

        task.Status = newStatus;
        await db.SaveChangesAsync();
        return ProductionTaskStatusResult.Ok(task);
    }

    public async Task<bool> AssignUserAsync(int taskId, int userId, int? assignedUserId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return false;

        var task = await db.ProductionTasks
            .Include(t => t.OrderItem)
                .ThenInclude(oi => oi.Deal)
            .FirstOrDefaultAsync(t => t.ProductionTaskId == taskId && t.OrderItem.Deal.CompanyId == companyId.Value);
        if (task == null)
            return false;

        task.AssignedUserId = assignedUserId;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<ProductionTask?> AddStageAsync(int orderItemId, int userId, string stageName, string executorType, int? assignedUserId, int? contractorId)
    {
        if (string.IsNullOrWhiteSpace(stageName))
            return null;

        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return null;

        var orderItem = await db.OrderItems
            .Include(oi => oi.Deal)
            .FirstOrDefaultAsync(oi => oi.OrderItemId == orderItemId && oi.Deal.CompanyId == companyId.Value);
        if (orderItem == null)
            return null;

        var nextOrder = await db.ProductionTasks
            .Where(task => task.OrderItemId == orderItemId)
            .Select(task => (int?)task.StageOrder)
            .MaxAsync() ?? 0;

        var task = new ProductionTask
        {
            OrderItemId = orderItemId,
            StageName = stageName.Trim(),
            StageOrder = nextOrder + 1,
            ExecutorType = string.IsNullOrWhiteSpace(executorType) ? "Internal" : executorType,
            AssignedUserId = assignedUserId,
            ContractorId = contractorId,
            Status = "Pending"
        };

        db.ProductionTasks.Add(task);
        await db.SaveChangesAsync();
        return task;
    }

    public async Task<bool> DeleteStageAsync(int taskId, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return false;

        var task = await db.ProductionTasks
            .Include(t => t.OrderItem)
                .ThenInclude(oi => oi.Deal)
            .FirstOrDefaultAsync(t => t.ProductionTaskId == taskId && t.OrderItem.Deal.CompanyId == companyId.Value);
        if (task == null)
            return false;

        if (task.Status == "Done")
            return false;

        db.ProductionTasks.Remove(task);
        await db.SaveChangesAsync();
        return true;
    }

    private static async Task<bool> IsFinalStageAsync(CrmContext db, ProductionTask task)
    {
        if (string.Equals(task.StageName, "ОТК", StringComparison.OrdinalIgnoreCase))
            return true;

        var maxOrder = await db.ProductionTasks
            .Where(t => t.OrderItemId == task.OrderItemId)
            .Select(t => (int?)t.StageOrder)
            .MaxAsync() ?? task.StageOrder;

        return task.StageOrder >= maxOrder;
    }

    private static async Task<int?> GetCompanyIdAsync(CrmContext db, int userId)
        => await db.Users
            .Where(user => user.Id == userId)
            .Select(user => user.CurrentCompanyId)
            .FirstOrDefaultAsync();
}
