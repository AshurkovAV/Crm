using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;

namespace Crm.Entity.Services;

public class ContractorPortalRepository : IContractorPortalRepository
{
    public async Task<ContractorPortalResult> GetByTokenAsync(Guid token)
    {
        await using var db = new CrmContext();

        var accessToken = await db.ContractorAccessTokens
            .Include(t => t.Contractor)
            .Include(t => t.ProductionTask).ThenInclude(pt => pt.OrderItem).ThenInclude(oi => oi.Deal)
            .FirstOrDefaultAsync(t => t.Token == token);

        if (accessToken == null)
            return new ContractorPortalResult { Status = ContractorPortalStatus.NotFound };

        if (accessToken.ExpiresAt.HasValue && accessToken.ExpiresAt.Value < DateTime.UtcNow)
            return new ContractorPortalResult { Status = ContractorPortalStatus.Expired };

        var task = accessToken.ProductionTask;
        var orderItem = task.OrderItem;
        var deal = orderItem.Deal;

        return new ContractorPortalResult
        {
            Status = ContractorPortalStatus.Ok,
            ContractorName = accessToken.Contractor.Name,
            StageName = task.StageName,
            Notes = task.Notes,
            OrderItemName = orderItem.Name,
            DealTitle = deal.Title,
            Deadline = deal.ExpectedCloseDate,
            TaskStatus = task.Status,
            StartedAt = task.StartedAt,
            CompletedAt = task.CompletedAt,
            TokenUsed = accessToken.UsedAt.HasValue
        };
    }

    public async Task<ContractorPortalActionResult> AcceptAsync(Guid token)
    {
        await using var db = new CrmContext();

        var accessToken = await db.ContractorAccessTokens
            .Include(t => t.ProductionTask)
            .FirstOrDefaultAsync(t => t.Token == token);

        if (accessToken == null)
            return new ContractorPortalActionResult { Status = ContractorPortalStatus.NotFound, Message = "Ссылка не найдена." };

        if (accessToken.ExpiresAt.HasValue && accessToken.ExpiresAt.Value < DateTime.UtcNow)
            return new ContractorPortalActionResult { Status = ContractorPortalStatus.Expired, Message = "Срок действия ссылки истёк." };

        var task = accessToken.ProductionTask;
        task.Status = "InProgress";
        task.StartedAt ??= DateTime.UtcNow;

        await db.SaveChangesAsync();
        return new ContractorPortalActionResult { Status = ContractorPortalStatus.Ok, Message = "Этап принят в работу." };
    }

    public async Task<ContractorPortalActionResult> ReadyAsync(Guid token)
    {
        await using var db = new CrmContext();

        var accessToken = await db.ContractorAccessTokens
            .Include(t => t.Contractor)
            .Include(t => t.ProductionTask).ThenInclude(pt => pt.OrderItem).ThenInclude(oi => oi.Deal)
            .FirstOrDefaultAsync(t => t.Token == token);

        if (accessToken == null)
            return new ContractorPortalActionResult { Status = ContractorPortalStatus.NotFound, Message = "Ссылка не найдена." };

        if (accessToken.ExpiresAt.HasValue && accessToken.ExpiresAt.Value < DateTime.UtcNow)
            return new ContractorPortalActionResult { Status = ContractorPortalStatus.Expired, Message = "Срок действия ссылки истёк." };

        var task = accessToken.ProductionTask;
        var now = DateTime.UtcNow;

        task.Status = "Done";
        task.CompletedAt = now;
        accessToken.UsedAt ??= now;

        // Ставим внутреннюю задачу водителю на забор детали у подрядчика.
        var newStatusId = await db.RefStatuses
            .Where(s => s.Name == "Новая")
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync() ?? 1;

        var driverTask = new TaskCrm
        {
            Name = $"Забрать деталь у подрядчика {accessToken.Contractor.Name}: {task.StageName} по изделию {task.OrderItem.Name}",
            Description = $"Сделка: {task.OrderItem.Deal.Title}. Подрядчик {accessToken.Contractor.Name} отгрузил деталь, требуется забрать.",
            Status = newStatusId,
            ProjectId = null,
            Assignee = null,
            Author = null,
            CreatedDate = now,
            ModifiedDate = now,
            Activity = now,
            IsOverdue = false
        };

        db.TaskCrms.Add(driverTask);

        await db.SaveChangesAsync();
        return new ContractorPortalActionResult { Status = ContractorPortalStatus.Ok, Message = "Готово! Диспетчер получил задачу на забор детали." };
    }
}
