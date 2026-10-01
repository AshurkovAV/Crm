using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;

namespace Crm.Entity.Services;

/// <summary>
/// Read-only запросы главной страницы. Один CrmContext на вызов, AsNoTracking, без N+1:
/// счётчики и вложенные агрегаты считаются в SQL.
/// </summary>
public class DashboardQueries : IDashboardQueries
{
    // Статусы сделок, которые считаются закрытыми (см. Views/Deals/Index.cshtml).
    private static readonly string[] ClosedDealStatuses = { "Успешно", "Проиграна" };
    private static readonly string[] DealStageOrder = { "Новая", "В работе", "Переговоры", "Успешно", "Проиграна" };

    // Статусы проектов, которые не считаются активными (см. Views/Project/Index.cshtml).
    private static readonly string[] InactiveProjectStatuses = { "Завершен", "Отменен" };

    private const string ProductionDone = "Done";

    public async Task<HomeDashboardData> GetHomeAsync(int userId, bool includeSales)
    {
        await using var db = new CrmContext();
        var result = new HomeDashboardData();

        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.FirstName, u.DisplayName, u.CurrentCompanyId })
            .FirstOrDefaultAsync();
        if (user == null)
            return result;

        result.FirstName = user.FirstName;
        result.DisplayName = user.DisplayName;

        // Текущая компания; если не выбрана — первая активная, где пользователь состоит.
        var companyId = user.CurrentCompanyId;
        if (!companyId.HasValue)
        {
            companyId = await db.CompanyUsers.AsNoTracking()
                .Where(cu => cu.UserId == userId && cu.IsActive)
                .OrderBy(cu => cu.JoinedDate)
                .Select(cu => (int?)cu.CompanyId)
                .FirstOrDefaultAsync();
        }
        result.CompanyId = companyId;

        // ---------- Статусы задач (справочник Ref_Statuses, несколько строк)
        var statuses = await db.RefStatuses.AsNoTracking().Select(s => new { s.Id, s.Name }).ToListAsync();
        var doneIds = statuses.Where(s => s.Name.StartsWith("Заверш", StringComparison.OrdinalIgnoreCase)).Select(s => s.Id).ToList();
        var cancelledIds = statuses.Where(s => s.Name.StartsWith("Отмен", StringComparison.OrdinalIgnoreCase)).Select(s => s.Id).ToList();
        var overdueIds = statuses.Where(s => s.Name.StartsWith("Просроч", StringComparison.OrdinalIgnoreCase)).Select(s => s.Id).ToList();
        if (doneIds.Count == 0) doneIds.Add(4);          // так же, как Views/Tasks/Index.cshtml
        if (cancelledIds.Count == 0) cancelledIds.Add(5);
        var closedIds = doneIds.Concat(cancelledIds).ToList();
        var statusNames = statuses.ToDictionary(s => s.Id, s => s.Name);

        var now = DateTime.Now;
        var weekAgo = now.AddDays(-7);

        // ---------- Мои задачи (исполнитель = я)
        var myTasks = db.TaskCrms.AsNoTracking().Where(t => t.Assignee == userId);
        var myOpen = myTasks.Where(t => t.Status == null || !closedIds.Contains(t.Status.Value));

        result.MyOpenTasks = await myOpen.CountAsync();
        result.MyOverdueTasks = await myOpen.CountAsync(t =>
            (t.Deadline != null && t.Deadline < now) || (t.Status != null && overdueIds.Contains(t.Status.Value)));
        // Дата завершения не хранится отдельно: ModifiedDate проставляется при каждом изменении, в т.ч. смене статуса.
        result.MyDoneLast7Days = await myTasks.CountAsync(t =>
            t.Status != null && doneIds.Contains(t.Status.Value) && t.ModifiedDate != null && t.ModifiedDate >= weekAgo);

        result.MyTasks = (await myOpen
                .OrderBy(t => t.Deadline == null)
                .ThenBy(t => t.Deadline)
                .ThenByDescending(t => t.ModifiedDate)
                .Take(7)
                .Select(t => new HomeTaskItem
                {
                    Id = t.Id,
                    Name = t.Name,
                    ProjectName = t.Project != null ? t.Project.Name : null,
                    Deadline = t.Deadline,
                    Priority = t.Priority,
                    Status = t.Status
                })
                .ToListAsync())
            .Select(t => { t.StatusName = t.Status.HasValue && statusNames.TryGetValue(t.Status.Value, out var n) ? n : null; return t; })
            .ToList();

        // ---------- Проекты (участие пользователя, как на странице «Проекты»)
        var myProjects = db.Projects.AsNoTracking()
            .Where(p => p.ProjectUsers.Any(pu => pu.UserId == userId && pu.IsActive))
            .Where(p => p.Status == null || !InactiveProjectStatuses.Contains(p.Status));

        result.ActiveProjects = await myProjects.CountAsync();
        result.Projects = await myProjects
            .OrderByDescending(p => p.ModifiedDate ?? p.CreatedDate)
            .Take(6)
            .Select(p => new HomeProjectItem
            {
                Id = p.Id,
                Name = p.Name,
                Status = p.Status,
                TotalTasks = p.TaskCrms.Count(t => t.Status == null || !cancelledIds.Contains(t.Status.Value)),
                DoneTasks = p.TaskCrms.Count(t => t.Status != null && doneIds.Contains(t.Status.Value)),
                Members = p.ProjectUsers.Count(pu => pu.IsActive)
            })
            .ToListAsync();

        if (!companyId.HasValue)
            return result;
        var cid = companyId.Value;

        // ---------- Команда
        result.CompanyName = await db.Companies.AsNoTracking()
            .Where(c => c.Id == cid).Select(c => c.Name).FirstOrDefaultAsync();

        var team = db.CompanyUsers.AsNoTracking().Where(cu => cu.CompanyId == cid && cu.IsActive);
        result.TeamCount = await team.CountAsync();
        result.Team = await team
            .OrderBy(cu => cu.UserId == userId ? 0 : 1)
            .ThenBy(cu => cu.JoinedDate)
            .Take(10)
            .Select(cu => new HomeTeamMember
            {
                UserId = cu.UserId,
                FirstName = cu.User.FirstName,
                LastName = cu.User.LastName,
                DisplayName = cu.User.DisplayName,
                Email = cu.User.DefaultEmail,
                Position = cu.Position ?? cu.User.Position,
                Role = cu.Role,
                DefaultAvatarId = cu.User.DefaultAvatarId,
                IsAvatarEmpty = cu.User.IsAvatarEmpty
            })
            .ToListAsync();

        if (!includeSales)
            return result;

        // ---------- Сделки по этапам (одна агрегирующая выборка)
        result.SalesLoaded = true;
        var stages = await db.Deals.AsNoTracking()
            .Where(d => d.CompanyId == cid)
            .GroupBy(d => d.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Amount = g.Sum(d => d.Amount) })
            .ToListAsync();

        result.DealStages = stages
            .Select(s => new HomeDealStage
            {
                Status = string.IsNullOrWhiteSpace(s.Status) ? "Без статуса" : s.Status,
                Count = s.Count,
                Amount = s.Amount,
                IsClosed = ClosedDealStatuses.Contains(s.Status)
            })
            .OrderBy(s => { var i = Array.IndexOf(DealStageOrder, s.Status); return i < 0 ? 99 : i; })
            .ThenBy(s => s.Status)
            .ToList();
        result.OpenDeals = result.DealStages.Where(s => !s.IsClosed).Sum(s => s.Count);
        result.OpenDealsAmount = result.DealStages.Where(s => !s.IsClosed).Sum(s => s.Amount);

        // ---------- Производство: изделия, у которых есть незавершённые этапы
        var companyItems = db.OrderItems.AsNoTracking().Where(oi => oi.Deal.CompanyId == cid);
        result.HasProduction = await companyItems.AnyAsync(oi => oi.ProductionTasks.Any());
        if (!result.HasProduction)
            return result;

        var inProduction = companyItems.Where(oi => oi.ProductionTasks.Any(pt => pt.Status != ProductionDone));
        var today = now.Date;
        result.OrdersInProduction = await inProduction.CountAsync();
        result.OrdersInProductionOverdue = await inProduction.CountAsync(oi =>
            oi.Deal.ExpectedCloseDate != null && oi.Deal.ExpectedCloseDate < today);
        result.Production = await inProduction
            .OrderBy(oi => oi.Deal.ExpectedCloseDate == null)
            .ThenBy(oi => oi.Deal.ExpectedCloseDate)
            .ThenByDescending(oi => oi.CreatedDate)
            .Take(5)
            .Select(oi => new HomeProductionItem
            {
                OrderItemId = oi.OrderItemId,
                Name = oi.Name,
                DealTitle = oi.Deal.Title,
                ClientName = oi.Deal.Client != null ? oi.Deal.Client.Name : oi.Deal.ClientName,
                TotalStages = oi.ProductionTasks.Count(),
                DoneStages = oi.ProductionTasks.Count(pt => pt.Status == ProductionDone),
                CurrentStage = oi.ProductionTasks
                    .Where(pt => pt.Status != ProductionDone)
                    .OrderBy(pt => pt.StageOrder)
                    .Select(pt => pt.StageName)
                    .FirstOrDefault(),
                DueDate = oi.Deal.ExpectedCloseDate
            })
            .ToListAsync();

        return result;
    }
}
