namespace Crm.Entity.DTO;

/// <summary>
/// Строка сметы по одному материалу (компоненту склада), с проверкой остатков.
/// </summary>
public class CalculationMaterialLineDto
{
    public int ComponentId { get; set; }

    public string Name { get; set; } = null!;

    public string Unit { get; set; } = null!;

    /// <summary>
    /// Требуемое количество компонента на ВЕСЬ расчёт (на Quantity изделий), результат QuantityFormula.
    /// </summary>
    public decimal RequiredQuantity { get; set; }

    /// <summary>
    /// Себестоимость единицы компонента (Component.CostPrice).
    /// </summary>
    public decimal UnitCostPrice { get; set; }

    /// <summary>
    /// RequiredQuantity * UnitCostPrice.
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Текущий остаток на складе (Component.StockQuantity).
    /// </summary>
    public decimal StockQuantity { get; set; }

    public bool IsShortage { get; set; }

    public string? WarningMessage { get; set; }
}
