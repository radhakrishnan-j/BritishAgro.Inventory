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

        // Convert dates to timestamps using local time
        var monthStartTimestamp = new DateTimeOffset(startDate.Date, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var monthEndTimestamp = new DateTimeOffset(endDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59), TimeSpan.Zero).ToUnixTimeMilliseconds();

        // Get all products
        var products = await dbContext.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

        if (products.Count == 0)
        {
            return new MonthlyReportData(year, month, new List<MonthlyStockReportItem>());
        }

        // Get ALL transactions (before and during the month)
        var allStockIn = await dbContext.StoreProductLots
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var allStockOut = await dbContext.ProductUsages
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Get all returns
        var allReturns = await dbContext.ProductReturns
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var reportItems = new List<MonthlyStockReportItem>();

        // Dictionary to track running balance per product across the month
        var productRunningBalance = new Dictionary<int, decimal>();

        // First pass: Calculate opening stock (before month starts)
        foreach (var product in products)
        {
            // Sum of receipts before month start
            var receiptsBeforeMonth = allStockIn
                .Where(x => x.ProductId == product.ItemId && x.ArrivalDate < monthStartTimestamp)
                .Sum(x => x.QuantityAvailable);

            // Sum of issues before month start
            var issuesBeforeMonth = allStockOut
                .Where(x => x.ProductId == product.ItemId && x.Date < monthStartTimestamp)
                .Sum(x => x.Issued ?? 0);

            // Sum of returns before month start
            var returnsBeforeMonth = allReturns
                .Where(x => x.ProductId == product.ItemId && x.Date < monthStartTimestamp)
                .Sum(x => x.QuantityReturned);

            // Opening balance = receipts - issues + returns (can be negative if issues > receipts)
            var openingBalance = receiptsBeforeMonth - issuesBeforeMonth + returnsBeforeMonth;

            productRunningBalance[product.ItemId] = Math.Max(0, openingBalance); // Never go below 0 for opening
        }

        // Get transactions for the month only
        var monthStockIn = allStockIn
            .Where(x => x.ArrivalDate >= monthStartTimestamp && x.ArrivalDate <= monthEndTimestamp)
            .ToList();

        var monthStockOut = allStockOut
            .Where(x => x.Date >= monthStartTimestamp && x.Date <= monthEndTimestamp)
            .ToList();

        var monthReturns = allReturns
            .Where(x => x.Date >= monthStartTimestamp && x.Date <= monthEndTimestamp)
            .ToList();

        // Generate daily entries for each product
        for (int day = 1; day <= DateTime.DaysInMonth(year, month); day++)
        {
            var currentDate = new DateTime(year, month, day);
            var dayStart = new DateTimeOffset(currentDate, TimeSpan.Zero).ToUnixTimeMilliseconds();
            var dayEnd = new DateTimeOffset(currentDate.AddHours(23).AddMinutes(59).AddSeconds(59), TimeSpan.Zero).ToUnixTimeMilliseconds();

            foreach (var product in products)
            {
                var openingStock = productRunningBalance[product.ItemId];

                // Get received for this day
                var receivedToday = monthStockIn
                    .Where(x => x.ProductId == product.ItemId && x.ArrivalDate >= dayStart && x.ArrivalDate <= dayEnd)
                    .Sum(x => x.QuantityAvailable);

                // Get issued for this day
                var issuedToday = monthStockOut
                    .Where(x => x.ProductId == product.ItemId && x.Date >= dayStart && x.Date <= dayEnd)
                    .Sum(x => x.Issued ?? 0);

                // Get returned for this day
                var returnedToday = monthReturns
                    .Where(x => x.ProductId == product.ItemId && x.Date >= dayStart && x.Date <= dayEnd)
                    .Sum(x => x.QuantityReturned);

                var closingStock = openingStock + receivedToday + returnedToday - issuedToday;

                // Update running balance for next day (allow negative for tracking discrepancies)
                productRunningBalance[product.ItemId] = closingStock;

                // Only include if there's activity (received, issued, or returned)
                if (receivedToday > 0 || issuedToday > 0 || returnedToday > 0)
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

        return new MonthlyReportData(year, month, reportItems.OrderBy(x => x.ProductName).ThenBy(x => x.Date).ToList());
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
