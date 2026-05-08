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
