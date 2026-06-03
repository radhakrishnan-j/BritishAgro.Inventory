using BritishAgro.Inventory.Data;
using Microsoft.EntityFrameworkCore;

namespace BritishAgro.Inventory.Services.Inventory;

public interface IInventoryReportService
{
    Task<DashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CurrentStockReportItem>> GetCurrentStockReportAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LowStockReportItem>> GetLowStockReportAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryStockSummary>> GetCategorySummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockMovementReportItem>> GetRecentMovementsAsync(int take = 20, CancellationToken cancellationToken = default);
    Task<MonthlyReportData> GetMonthlyStockReportAsync(int year, int month, CancellationToken cancellationToken = default);
}

public sealed class InventoryReportService(ApplicationDbContext dbContext) : IInventoryReportService
{
    public async Task<DashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var products = await dbContext.Products.AsNoTracking().ToListAsync(cancellationToken);
        var totalStock = await dbContext.StoreProductLots.AsNoTracking().SumAsync(x => x.QuantityAvailable, cancellationToken);
        var lowStockCount = await dbContext.Products
            .AsNoTracking()
            .CountAsync(product => product.IsActive
                && product.ReorderLevel.HasValue
                && dbContext.StoreProductLots
                    .Where(lot => lot.ProductId == product.ItemId)
                    .Sum(lot => lot.QuantityAvailable) <= product.ReorderLevel.Value, cancellationToken);

        return new DashboardSummary(
            await dbContext.Categories.AsNoTracking().CountAsync(cancellationToken),
            products.Count,
            totalStock,
            lowStockCount,
            products.Count(product => product.IsActive));
    }

    public async Task<IReadOnlyList<CurrentStockReportItem>> GetCurrentStockReportAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.StoreStocks)
            .OrderBy(product => product.Name)
            .Select(product => new CurrentStockReportItem(
                product.ItemId,
                product.Name,
                product.Category != null ? product.Category.Name : "Unassigned",
                product.UnitOfMeasurement,
                product.StoreStocks.Sum(lot => lot.QuantityAvailable),
                product.ReorderLevel,
                product.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LowStockReportItem>> GetLowStockReportAsync(CancellationToken cancellationToken = default)
    {
        var products = await dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.StoreStocks)
            .Where(product => product.IsActive && product.ReorderLevel.HasValue)
            .ToListAsync(cancellationToken);

        return products
            .Select(product => new LowStockReportItem(
                product.ItemId,
                product.Name,
                product.Category != null ? product.Category.Name : "Unassigned",
                product.StoreStocks.Sum(lot => lot.QuantityAvailable),
                product.ReorderLevel!.Value,
                product.UnitOfMeasurement))
            .Where(item => item.CurrentQuantity <= item.ReorderLevel)
            .OrderBy(item => item.CurrentQuantity)
            .ToList();
    }

    public async Task<IReadOnlyList<CategoryStockSummary>> GetCategorySummaryAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .Include(category => category.Products)
            .ThenInclude(product => product.StoreStocks)
            .OrderBy(category => category.Name)
            .Select(category => new CategoryStockSummary(
                category.Name,
                category.Products.Count,
                category.Products.SelectMany(product => product.StoreStocks).Sum(lot => lot.QuantityAvailable),
                category.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockMovementReportItem>> GetRecentMovementsAsync(int take = 20, CancellationToken cancellationToken = default)
    {
        var stockEntries = await dbContext.StoreProductLots
            .AsNoTracking()
            .Include(x => x.Product)
            .Select(x => new StockMovementReportItem(
                x.Product.Name,
                "Stock In",
                x.QuantityAvailable,
                x.ArrivalDate,
                $"Lot #{x.ProductLotId}"))
            .ToListAsync(cancellationToken);

        var usages = await dbContext.ProductUsages
            .AsNoTracking()
            .Include(x => x.Product)
            .Select(x => new StockMovementReportItem(
                x.Product.Name,
                "Usage",
                -(x.Issued ?? 0),
                x.Date,
                x.Remarks))
            .ToListAsync(cancellationToken);

        var returns = await dbContext.ProductReturns
            .AsNoTracking()
            .Include(x => x.Product)
            .Select(x => new StockMovementReportItem(
                x.Product.Name,
                "Return",
                x.QuantityReturned,
                x.Date,
                x.Remarks))
            .ToListAsync(cancellationToken);

        return stockEntries
            .Concat(usages)
            .Concat(returns)
            .OrderByDescending(item => item.Date)
            .Take(take)
            .ToList();
    }

    public async Task<MonthlyReportData> GetMonthlyStockReportAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        var startTimestamp = new DateTimeOffset(startDate.Date, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var endTimestamp = new DateTimeOffset(endDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59), TimeSpan.Zero).ToUnixTimeMilliseconds();

        // Get all products
        var products = await dbContext.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Include(p => p.StoreStocks)
            .Include(p => p.ProductUsages)
            .ToListAsync(cancellationToken);

        // Get all stock movements for the month
        var stockIn = await dbContext.StoreProductLots
            .AsNoTracking()
            .Where(x => x.ArrivalDate >= startTimestamp && x.ArrivalDate <= endTimestamp)
            .ToListAsync(cancellationToken);

        var stockOut = await dbContext.ProductUsages
            .AsNoTracking()
            .Where(x => x.Date >= startTimestamp && x.Date <= endTimestamp)
            .ToListAsync(cancellationToken);

        var reportItems = new List<MonthlyStockReportItem>();

        // Generate daily entries for each product
        for (int day = 1; day <= DateTime.DaysInMonth(year, month); day++)
        {
            var currentDate = new DateTime(year, month, day);
            var dayStart = new DateTimeOffset(currentDate, TimeSpan.Zero).ToUnixTimeMilliseconds();
            var dayEnd = new DateTimeOffset(currentDate.AddHours(23).AddMinutes(59).AddSeconds(59), TimeSpan.Zero).ToUnixTimeMilliseconds();

            foreach (var product in products)
            {
                // Calculate opening stock (stock from previous day)
                var openingStock = await GetStockAtDateAsync(product.ItemId, dayStart, cancellationToken);

                // Get received for this day
                var receivedToday = stockIn
                    .Where(x => x.ProductId == product.ItemId && x.ArrivalDate >= dayStart && x.ArrivalDate <= dayEnd)
                    .Sum(x => x.QuantityAvailable);

                // Get issued for this day
                var issuedToday = stockOut
                    .Where(x => x.ProductId == product.ItemId && x.Date >= dayStart && x.Date <= dayEnd)
                    .Sum(x => x.Issued ?? 0);

                var closingStock = openingStock + receivedToday - issuedToday;

                // Only include if there's activity or opening/closing stock
                if (receivedToday > 0 || issuedToday > 0 || openingStock > 0 || closingStock > 0)
                {
                    reportItems.Add(new MonthlyStockReportItem(
                        product.ItemId,
                        product.Name,
                        product.UnitOfMeasurement ?? "Unit",
                        currentDate,
                        openingStock,
                        receivedToday,
                        issuedToday,
                        closingStock));
                }
            }
        }

        return new MonthlyReportData(year, month, reportItems.OrderBy(x => x.Date).ThenBy(x => x.ProductName).ToList());
    }

    private async Task<decimal> GetStockAtDateAsync(int productId, long timestamp, CancellationToken cancellationToken)
    {
        // Get all stock receipts up to this date
        var received = await dbContext.StoreProductLots
            .AsNoTracking()
            .Where(x => x.ProductId == productId && x.ArrivalDate < timestamp)
            .SumAsync(x => x.QuantityAvailable, cancellationToken);

        // Get all stock issues up to this date
        var issued = await dbContext.ProductUsages
            .AsNoTracking()
            .Where(x => x.ProductId == productId && x.Date < timestamp)
            .SumAsync(x => x.Issued ?? 0, cancellationToken);

        return received - issued;
    }
}

public sealed record DashboardSummary(
    int TotalCategories,
    int TotalProducts,
    decimal TotalQuantityOnHand,
    int LowStockItems,
    int ActiveProducts);

public sealed record CurrentStockReportItem(
    int ProductId,
    string ProductName,
    string CategoryName,
    string? UnitOfMeasurement,
    decimal QuantityOnHand,
    decimal? ReorderLevel,
    bool IsActive);

public sealed record LowStockReportItem(
    int ProductId,
    string ProductName,
    string CategoryName,
    decimal CurrentQuantity,
    decimal ReorderLevel,
    string? UnitOfMeasurement);

public sealed record CategoryStockSummary(
    string CategoryName,
    int ProductCount,
    decimal QuantityOnHand,
    bool IsActive);

public sealed record StockMovementReportItem(
    string ProductName,
    string MovementType,
    decimal Quantity,
    long Date,
    string? Remarks);
