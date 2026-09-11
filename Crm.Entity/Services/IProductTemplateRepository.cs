using Crm.Entity.ModelsCrm;

namespace Crm.Entity.Services;

public interface IProductTemplateRepository
{
    Task<List<ProductTemplate>> GetByUserAsync(int userId);

    /// <summary>
    /// Возвращает шаблон изделия вместе с составом BOM (ProductTemplateComponents -> Component),
    /// необходимым калькулятору для расчёта себестоимости и проверки остатков.
    /// </summary>
    Task<ProductTemplate?> GetWithComponentsAsync(int id, int userId);

    Task<ProductTemplate?> GetAsync(int id, int userId);
    Task<ProductTemplate> AddAsync(ProductTemplate template, int userId);
    Task<bool> UpdateAsync(ProductTemplate template, int userId);
    Task<bool> DeleteAsync(int id, int userId);
}
