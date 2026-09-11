using Crm.Entity.ModelsCrm;

namespace Crm.Entity.Services;

/// <summary>
/// Изделия в составе сделки (OrderItem). Company-scoping идёт через Deal.CompanyId, т.к. у OrderItem
/// нет собственного CompanyId. Интерфейс стабильный: методы GetByDealAsync/AddAsync используются
/// другими модулями (цех генерирует производственные этапы по каждому OrderItem).
/// </summary>
public interface IOrderItemRepository
{
    Task<List<OrderItem>> GetByDealAsync(int dealId, int userId);
    Task<OrderItem?> GetAsync(int id, int userId);
    Task<OrderItem?> AddAsync(OrderItem orderItem, int userId);
    Task<bool> UpdateAsync(OrderItem orderItem, int userId);
    Task<bool> DeleteAsync(int id, int userId);
}
