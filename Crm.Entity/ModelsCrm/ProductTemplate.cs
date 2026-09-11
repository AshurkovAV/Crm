using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

/// <summary>
/// Шаблон изделия для сметного калькулятора (Модуль А ТЗ).
/// FormulaExpression вычисляется движком NCalc, переменные: Width, Height, Depth, Quantity
/// и стоимости компонентов, привязанных через ProductTemplateComponents.
/// </summary>
public partial class ProductTemplate
{
    public int ProductTemplateId { get; set; }

    public int CompanyId { get; set; }

    public string Name { get; set; } = null!;

    public string? Category { get; set; }

    public string Unit { get; set; } = null!;

    public string FormulaExpression { get; set; } = null!;

    public decimal? DefaultMarginPercent { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<ProductTemplateComponent> ProductTemplateComponents { get; set; } = new List<ProductTemplateComponent>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
