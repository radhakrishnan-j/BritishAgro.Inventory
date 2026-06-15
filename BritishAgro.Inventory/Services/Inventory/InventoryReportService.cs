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
    Task<MonthlyReportData> GetMonthlyStockReportAsync(int year, int month, int? browserOffsetMinutes = null, CancellationToken cancellationToken = default);
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

    public async Task<MonthlyReportData> GetMonthlyStockReportAsync(int year, int month, int? browserOffsetMinutes = null, CancellationToken cancellationToken = default)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var offset = TimeSpan.FromMinutes(-(browserOffsetMinutes ?? 0));

        // Convert local month boundaries to UTC timestamps
        var monthStartTimestamp = new DateTimeOffset(startDate.Date, offset).ToUnixTimeMilliseconds();
        var monthEndTimestamp = new DateTimeOffset(endDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59), offset).ToUnixTimeMilliseconds();

        // Get all products
        var products = await dbContext.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

        if (products.Count == 0)
        {
            return new MonthlyReportData(year, month, new List<MonthlyStockReportItem>());
        }

        // Get ALL transactions to reconstruct historical balances
        var allStockLots = await dbContext.StoreProductLots
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var allProductUsages = await dbContext.ProductUsages
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var allProductReturns = await dbContext.ProductReturns
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Transactions DURING the month only
        var monthTransactionsIn = allStockLots
            .Where(x => x.ArrivalDate >= monthStartTimestamp && x.ArrivalDate <= monthEndTimestamp)
            .ToList();

        var monthStockOut = allProductUsages
            .Where(x => x.Date >= monthStartTimestamp && x.Date <= monthEndTimestamp)
            .ToList();

        var monthReturns = allProductReturns
            .Where(x => x.Date >= monthStartTimestamp && x.Date <= monthEndTimestamp)
            .ToList();

        var reportItems = new List<MonthlyStockReportItem>();
        var productRunningBalance = new Dictionary<int, decimal>();

        // CALCULATE OPENING STOCK for each product at month start
        // Opening Stock = (All received before month) - (All issued before month) + (All returned before month)
        foreach (var product in products)
        {
            var receivedBeforeMonth = allStockLots
                .Where(x => x.ProductId == product.ItemId && x.ArrivalDate < monthStartTimestamp)
                .Sum(x => x.QuantityReceived > 0 ? x.QuantityReceived : x.QuantityAvailable);

            var issuedBeforeMonth = allProductUsages
                .Where(x => x.ProductId == product.ItemId && x.Date < monthStartTimestamp)
                .Sum(x => x.Issued ?? 0);

            var returnedBeforeMonth = allProductReturns
                .Where(x => x.ProductId == product.ItemId && x.Date < monthStartTimestamp)
                .Sum(x => x.QuantityReturned);

            var openingStock = receivedBeforeMonth - issuedBeforeMonth + returnedBeforeMonth;
            productRunningBalance[product.ItemId] = openingStock;
        }

        // Generate daily entries for each product
        for (int day = 1; day <= DateTime.DaysInMonth(year, month); day++)
        {
            var currentDate = new DateTime(year, month, day);
            var dayStart = new DateTimeOffset(currentDate, offset).ToUnixTimeMilliseconds();
            var dayEnd = new DateTimeOffset(currentDate.AddHours(23).AddMinutes(59).AddSeconds(59), offset).ToUnixTimeMilliseconds();

            foreach (var product in products)
            {
                var openingStock = productRunningBalance[product.ItemId];

                // RECEIVED during this day = new lots added on this day
                var receivedToday = monthTransactionsIn
                    .Where(x => x.ProductId == product.ItemId && x.ArrivalDate >= dayStart && x.ArrivalDate <= dayEnd)
                    .Sum(x => x.QuantityReceived > 0 ? x.QuantityReceived : x.QuantityAvailable);

                // ISSUED during this day
                var issuedToday = monthStockOut
                    .Where(x => x.ProductId == product.ItemId && x.Date >= dayStart && x.Date <= dayEnd)
                    .Sum(x => x.Issued ?? 0);

                // RETURNED during this day
                var returnedToday = monthReturns
                    .Where(x => x.ProductId == product.ItemId && x.Date >= dayStart && x.Date <= dayEnd)
                    .Sum(x => x.QuantityReturned);

                var closingStock = openingStock + receivedToday + returnedToday - issuedToday;

                // Update running balance for next day
                productRunningBalance[product.ItemId] = closingStock;

                // Only include if there's activity (received, issued, or returned)
                if (receivedToday > 0 || issuedToday > 0 || returnedToday > 0)
                {
                    // load category name for grouping
                    var categoryName = (await dbContext.Products.Where(p => p.ItemId == product.ItemId).Select(p => p.Category != null ? p.Category.Name : "Unassigned").FirstOrDefaultAsync(cancellationToken)) ?? "Unassigned";

                    reportItems.Add(new MonthlyStockReportItem(
                        product.ItemId,
                        product.Name,
                        categoryName,
                        product.UnitOfMeasurement ?? "Unit",
                        currentDate,
                        dayStart,
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
