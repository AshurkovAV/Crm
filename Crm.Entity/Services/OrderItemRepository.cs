using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;

namespace Crm.Entity.Services;

public class OrderItemRepository : IOrderItemRepository
{
    public async Task<List<OrderItem>> GetByDealAsync(int dealId, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return new List<OrderItem>();

        return await db.OrderItems
            .AsNoTracking()
            .Include(item => item.ProductTemplate)
            .Where(item => item.DealId == dealId && item.Deal.CompanyId == companyId.Value)
            .OrderBy(item => item.CreatedDate)
            .ToListAsync();
    }

    public async Task<OrderItem?> GetAsync(int id, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            return null;

        return await db.OrderItems
            .Include(item => item.ProductTemplate)
            .FirstOrDefaultAsync(item => item.OrderItemId == id && item.Deal.CompanyId == companyId.Value);
    }

    public async Task<OrderItem?> AddAsync(OrderItem orderItem, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await GetCompanyIdAsync(db, userId);
        if (!companyId.HasValue)
            throw new InvalidOperationException("У пользователя не выбрана текущая компания.");

        var dealOwnedByCompany = await db.Deals
            .AnyAsync(deal => deal.Id == orderItem.DealId && deal.CompanyId == companyId.Value);
        if (!dealOwnedByCompany)
            return null;

        orderItem.CreatedDate = orderItem.CreatedDate == default ? DateTime.UtcNow : orderItem.CreatedDate;
        db.OrderItems.Add(orderItem);
        await db.SaveChangesAsync();
        return orderItem;
    }

    public async Task<bool> UpdateAsync(OrderItem orderItem, int userId)
    {
        var existing = await GetAsync(orderItem.OrderItemId, userId);
        if (existing == null)
            return false;

        await using var db = new CrmContext();
        existing.Name = orderItem.Name;
        existing.Quantity = orderItem.Quantity;
        existing.Width = orderItem.Width;
        existing.Height = orderItem.Height;
        existing.Depth = orderItem.Depth;
        existing.CostPrice = orderItem.CostPrice;
        existing.MarginPercent = orderItem.MarginPercent;
        existing.Price = orderItem.Price;
        existing.Status = orderItem.Status;
        existing.ProductTemplateId = orderItem.ProductTemplateId;
        db.OrderItems.Update(existing);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var existing = await GetAsync(id, userId);
        if (existing == null)
            return false;

        await using var db = new CrmContext();
        db.OrderItems.Remove(existing);
        await db.SaveChangesAsync();
        return true;
    }

    private static async Task<int?> GetCompanyIdAsync(CrmContext db, int userId)
        => await db.Users
            .Where(user => user.Id == userId)
            .Select(user => user.CurrentCompanyId)
            .FirstOrDefaultAsync();
}
