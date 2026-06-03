namespace BritishAgro.Inventory.Services.Inventory;

public record MonthlyStockReportItem(
    int ProductId,
    string ProductName,
    string UnitOfMeasurement,
    DateTime Date,
    decimal OpeningStock,
    decimal Received,
    decimal Issued,
    decimal ClosingStock);

public record MonthlyReportData(
    int Year,
    int Month,
    IReadOnlyList<MonthlyStockReportItem> ReportItems);
