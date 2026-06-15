namespace BritishAgro.Inventory.Services.Inventory;

public record MonthlyStockReportItem(
    int ProductId,
    string ProductName,
    string CategoryName,
    string UnitOfMeasurement,
    DateTime Date,
    long UtcDayTimestamp,
    decimal OpeningStock,
    decimal Received,
    decimal Issued,
    decimal ClosingStock);

public record MonthlyReportData(
    int Year,
    int Month,
    IReadOnlyList<MonthlyStockReportItem> ReportItems);
