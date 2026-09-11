using Crm.Core.Infrastructure;
using Crm.Entity.DTO;

namespace Crm.Entity.Services;

/// <summary>
/// Сметный движок Модуля А ("Умный калькулятор"). Считает себестоимость/маржу/цену изделия
/// по шаблону (ProductTemplate.FormulaExpression, NCalc) и проверяет остатки компонентов на складе.
/// </summary>
public interface ICalculationService
{
    Task<TransactionResult<CalculationResultDto>> ComputeAsync(CalculationRequestDto request, int userId);
}
