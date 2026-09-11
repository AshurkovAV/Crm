using Crm.Core.Infrastructure;
using Crm.Entity.DTO;
using Crm.Entity.ModelsCrm;
using NCalc;

namespace Crm.Entity.Services;

/// <summary>
/// Реализация Модуля А ТЗ ("Умный калькулятор"). Формулы (ProductTemplate.FormulaExpression и
/// ProductTemplateComponent.QuantityFormula) — обычные строки, интерпретируемые движком NCalc
/// (пакет NCalcSync, пространство имён NCalc), без перекомпиляции кода.
///
/// СОГЛАШЕНИЕ О ПЕРЕМЕННЫХ (важно для тех, кто заполняет справочники шаблонов):
/// - Width, Height, Depth, Quantity — параметры запроса, доступны И в FormulaExpression шаблона,
///   И в QuantityFormula каждой строки BOM. Depth, если не передан, подставляется как 0.
/// - QuantityFormula каждой строки BOM должна возвращать количество компонента, требуемое НА ВЕСЬ
///   расчёт (т.е. на Quantity изделий), а не на одну штуку — например "Width*Height*Quantity" для
///   расхода стекла или "4*Quantity" для количества ручек. Это количество и сравнивается с остатком
///   на складе (Component.StockQuantity) для предупреждения "Требуется закупка".
/// - MaterialsCost — готовая переменная, подставляемая движком в FormulaExpression шаблона: это сумма
///   (QuantityFormula_i * Component_i.CostPrice) по всем строкам BOM, т.е. суммарная стоимость
///   материалов на весь расчёт. Шаблон может использовать её напрямую (например формула "MaterialsCost"
///   или "MaterialsCost * 1.3" для наценки на материалы) либо полностью игнорировать и считать по
///   геометрии, как в примере ТЗ: "(Width * Height * PriceGlass) + ((Width + Height) * 2 * PriceFrame)".
/// - CostPrice (результат) — это ПРЯМОЙ результат вычисления FormulaExpression, без каких-либо
///   дополнительных домножений движком. Если формула должна возвращать себестоимость на весь заказ
///   (а не на одно изделие), автор формулы обязан сам домножить нужную часть выражения на Quantity —
///   Quantity ему для этого и передаётся. Такое решение исключает двойной счёт (когда и формула,
///   и движок домножают на Quantity) и даёт полную гибкость: часть выражения может линейно зависеть
///   от Quantity, а часть — нет (например разовые работы по проекту).
/// </summary>
public class CalculationService : ICalculationService
{
    private readonly IProductTemplateRepository _productTemplateRepository;

    public CalculationService(IProductTemplateRepository productTemplateRepository)
    {
        _productTemplateRepository = productTemplateRepository;
    }

    public async Task<TransactionResult<CalculationResultDto>> ComputeAsync(CalculationRequestDto request, int userId)
    {
        var result = new TransactionResult<CalculationResultDto>();

        if (request.ProductTemplateId <= 0)
        {
            result.AddError("Не указан шаблон изделия.");
            return result;
        }

        if (request.Width <= 0 || request.Height <= 0)
        {
            result.AddError("Ширина и высота изделия должны быть больше нуля.");
            return result;
        }

        if (request.Quantity <= 0)
        {
            result.AddError("Количество должно быть больше нуля.");
            return result;
        }

        var template = await _productTemplateRepository.GetWithComponentsAsync(request.ProductTemplateId, userId);
        if (template == null)
        {
            result.AddError("Шаблон изделия не найден или не принадлежит вашей текущей компании.");
            return result;
        }

        var depth = request.Depth ?? 0m;

        var dto = new CalculationResultDto
        {
            ProductTemplateId = template.ProductTemplateId,
            ProductTemplateName = template.Name,
            Width = request.Width,
            Height = request.Height,
            Depth = request.Depth,
            Quantity = request.Quantity,
            SuggestedMarginPercent = template.DefaultMarginPercent ?? 0m
        };

        // 1) Считаем расход и стоимость каждого компонента BOM, заодно проверяя остатки на складе.
        decimal materialsCost = 0m;
        foreach (var bomLine in template.ProductTemplateComponents)
        {
            decimal requiredQuantity;
            try
            {
                requiredQuantity = EvaluateFormula(bomLine.QuantityFormula, request.Width, request.Height, depth, request.Quantity, null);
            }
            catch (Exception ex)
            {
                result.AddError($"Ошибка в формуле расхода компонента \"{bomLine.Component.Name}\" (QuantityFormula = \"{bomLine.QuantityFormula}\"): {ex.Message}");
                return result;
            }

            var component = bomLine.Component;
            var totalCost = requiredQuantity * component.CostPrice;
            materialsCost += totalCost;

            var isShortage = requiredQuantity > component.StockQuantity;
            var line = new CalculationMaterialLineDto
            {
                ComponentId = component.ComponentId,
                Name = component.Name,
                Unit = component.Unit,
                RequiredQuantity = requiredQuantity,
                UnitCostPrice = component.CostPrice,
                TotalCost = totalCost,
                StockQuantity = component.StockQuantity,
                IsShortage = isShortage,
                WarningMessage = isShortage
                    ? $"Внимание: {component.Name} осталось на {component.StockQuantity} {component.Unit}. Требуется закупка."
                    : null
            };

            dto.Materials.Add(line);
            if (isShortage)
                dto.Warnings.Add(line.WarningMessage!);
        }

        dto.MaterialsCost = materialsCost;
        dto.HasShortage = dto.Materials.Any(m => m.IsShortage);

        // 2) Считаем итоговую себестоимость по формуле шаблона.
        try
        {
            dto.CostPrice = EvaluateFormula(template.FormulaExpression, request.Width, request.Height, depth, request.Quantity, materialsCost);
        }
        catch (Exception ex)
        {
            result.AddError($"Ошибка в формуле себестоимости шаблона \"{template.Name}\" (FormulaExpression = \"{template.FormulaExpression}\"): {ex.Message}");
            return result;
        }

        if (dto.CostPrice < 0)
            dto.CostPrice = 0;

        dto.SuggestedPrice = Math.Round(dto.CostPrice * (1 + dto.SuggestedMarginPercent / 100m), 2);

        result.Data = dto;
        return result;
    }

    /// <summary>
    /// Вычисляет NCalc-выражение с переменными Width/Height/Depth/Quantity (и MaterialsCost, если передан).
    /// Пробрасывает исключение наверх понятным сообщением — вызывающий код решает, как это отобразить.
    /// </summary>
    private static decimal EvaluateFormula(string formula, decimal width, decimal height, decimal depth, decimal quantity, decimal? materialsCost)
    {
        if (string.IsNullOrWhiteSpace(formula))
            throw new InvalidOperationException("Формула не задана.");

        var expression = new Expression(formula);
        expression.Parameters["Width"] = width;
        expression.Parameters["Height"] = height;
        expression.Parameters["Depth"] = depth;
        expression.Parameters["Quantity"] = quantity;
        if (materialsCost.HasValue)
            expression.Parameters["MaterialsCost"] = materialsCost.Value;

        object? evaluated;
        try
        {
            evaluated = expression.Evaluate();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(ex.Message, ex);
        }

        if (evaluated == null)
            throw new InvalidOperationException("Формула вернула пустой результат.");

        try
        {
            return Convert.ToDecimal(evaluated);
        }
        catch (Exception)
        {
            throw new InvalidOperationException($"Формула вернула нечисловой результат: {evaluated}.");
        }
    }
}
