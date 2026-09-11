namespace Crm.Entity.DTO;

/// <summary>
/// Результат расчёта сметы: себестоимость, маржа, итоговая цена и список материалов
/// с предупреждениями по остаткам на складе (см. CalculationService для деталей расчёта).
/// </summary>
public class CalculationResultDto
{
    public int ProductTemplateId { get; set; }

    public string ProductTemplateName { get; set; } = null!;

    public decimal Width { get; set; }

    public decimal Height { get; set; }

    public decimal? Depth { get; set; }

    public decimal Quantity { get; set; }

    /// <summary>
    /// Суммарная себестоимость материалов на ВЕСЬ расчёт (сумма QuantityFormula_i(Width,Height,Depth,Quantity) * CostPrice_i
    /// по всем компонентам BOM шаблона). Доступна формуле шаблона как готовая переменная "MaterialsCost".
    /// </summary>
    public decimal MaterialsCost { get; set; }

    /// <summary>
    /// Итоговая себестоимость на весь расчёт — прямой результат
    /// FormulaExpression(Width, Height, Depth, Quantity, MaterialsCost). Формула сама отвечает за то,
    /// как использовать Quantity (движок ничего дополнительно не домножает).
    /// </summary>
    public decimal CostPrice { get; set; }

    public decimal SuggestedMarginPercent { get; set; }

    /// <summary>
    /// CostPrice * (1 + SuggestedMarginPercent / 100).
    /// </summary>
    public decimal SuggestedPrice { get; set; }

    public List<CalculationMaterialLineDto> Materials { get; set; } = new();

    public bool HasShortage { get; set; }

    public List<string> Warnings { get; set; } = new();
}
