using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;

namespace Crm.Entity.Services;

public class ContractorAssignmentRepository : IContractorAssignmentRepository
{
    public async Task<List<AssignableStageRow>> GetAssignableStagesAsync(int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return new List<AssignableStageRow>();

        return await db.ProductionTasks
            .AsNoTracking()
            .Include(task => task.OrderItem).ThenInclude(item => item.Deal)
            .Include(task => task.Contractor)
            .Where(task => task.OrderItem.Deal.CompanyId == companyId.Value && task.Status != "Done")
            .OrderBy(task => task.StageOrder)
            .Select(task => new AssignableStageRow
            {
                ProductionTaskId = task.ProductionTaskId,
                StageName = task.StageName,
                Status = task.Status,
                ExecutorType = task.ExecutorType,
                ContractorName = task.Contractor != null ? task.Contractor.Name : null,
                OrderItemName = task.OrderItem.Name,
                DealTitle = task.OrderItem.Deal.Title
            })
            .ToListAsync();
    }

    public async Task<ContractorAssignResult?> AssignAndCreateLinkAsync(int productionTaskId, int contractorId, int userId, int? expiresInDays)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return null;

        var task = await db.ProductionTasks
            .Include(t => t.OrderItem).ThenInclude(i => i.Deal)
            .FirstOrDefaultAsync(t => t.ProductionTaskId == productionTaskId && t.OrderItem.Deal.CompanyId == companyId.Value);
        if (task == null)
            return null;

        var contractor = await db.Contractors
            .FirstOrDefaultAsync(c => c.ContractorId == contractorId && c.CompanyId == companyId.Value);
        if (contractor == null)
            return null;

        task.ExecutorType = "External";
        task.ContractorId = contractor.ContractorId;

        var now = DateTime.UtcNow;
        var token = new ContractorAccessToken
        {
            ContractorId = contractor.ContractorId,
            ProductionTaskId = task.ProductionTaskId,
            Token = Guid.NewGuid(),
            CreatedAt = now,
            ExpiresAt = expiresInDays.HasValue ? now.AddDays(expiresInDays.Value) : now.AddDays(30)
        };

        db.ContractorAccessTokens.Add(token);
        await db.SaveChangesAsync();

        return new ContractorAssignResult
        {
            Token = token.Token,
            RelativeUrl = $"/contractor/{token.Token}",
            CreatedAt = token.CreatedAt,
            ExpiresAt = token.ExpiresAt
        };
    }

    private static async Task<int?> GetCompanyIdAsync(CrmContext db, int userId)
        => await db.Users
            .Where(user => user.Id == userId)
            .Select(user => user.CurrentCompanyId)
            .FirstOrDefaultAsync();
}
