namespace Crm.Models.Calculator
{
    /// <summary>
    /// Плоское представление Component для DevExtreme DataGrid (с именем поставщика для отображения).
    /// </summary>
    public sealed class ComponentRow
    {
        public int ComponentId { get; set; }
        public string Name { get; set; } = null!;
        public string Unit { get; set; } = null!;
        public decimal CostPrice { get; set; }
        public decimal StockQuantity { get; set; }
        public decimal? ReorderLevel { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public bool IsActive { get; set; }
    }
}
