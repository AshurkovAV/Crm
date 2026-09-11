using System;

namespace Crm.Entity.ModelsCrm;

/// <summary>
/// Строка спецификации (BOM) шаблона изделия: сколько компонента расходуется.
/// QuantityFormula — выражение NCalc от Width/Height/Depth/Quantity (например "Width*Height"),
/// используется калькулятором для проверки остатков на складе.
/// </summary>
public partial class ProductTemplateComponent
{
    public int Id { get; set; }

    public int ProductTemplateId { get; set; }

    public int ComponentId { get; set; }

    public string QuantityFormula { get; set; } = null!;

    public string? Notes { get; set; }

    public virtual ProductTemplate ProductTemplate { get; set; } = null!;

    public virtual Component Component { get; set; } = null!;
}
