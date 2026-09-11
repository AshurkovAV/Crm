using Crm.Entity.ModelsCrm;

namespace Crm.Entity.Services;

/// <summary>
/// CRUD для строк BOM (состава) шаблона изделия. Компания определяется через ProductTemplate.CompanyId,
/// т.к. у ProductTemplateComponent нет собственного CompanyId.
/// </summary>
public interface IProductTemplateComponentRepository
{
    Task<List<ProductTemplateComponent>> GetByTemplateAsync(int productTemplateId, int userId);
    Task<ProductTemplateComponent?> GetAsync(int id, int userId);
    Task<ProductTemplateComponent?> AddAsync(ProductTemplateComponent item, int userId);
    Task<bool> UpdateAsync(ProductTemplateComponent item, int userId);
    Task<bool> DeleteAsync(int id, int userId);
}
