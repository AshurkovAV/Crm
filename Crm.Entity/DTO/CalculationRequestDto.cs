namespace Crm.Entity.DTO;

/// <summary>
/// Входные параметры расчёта сметы (Модуль А ТЗ, POST /api/v1/calculations/compute).
/// </summary>
public class CalculationRequestDto
{
    public int ProductTemplateId { get; set; }

    public decimal Width { get; set; }

    public decimal Height { get; set; }

    public decimal? Depth { get; set; }

    public decimal Quantity { get; set; } = 1;
}
