using BritishAgro.Inventory.Data;
using Microsoft.EntityFrameworkCore;

namespace BritishAgro.Inventory.Services.Inventory;

public interface IInventoryTransactionService
{
    Task<IReadOnlyList<StoreProductLot>> GetStockLotsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductUsage>> GetUsageHistoryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductReturn>> GetReturnHistoryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductUsage>> GetUsagesForProductAsync(int? productId = null, CancellationToken cancellationToken = default);
    Task<StoreProductLot> AddStockAsync(StoreProductLot lot, CancellationToken cancellationToken = default);
    Task<ProductUsage> RecordUsageAsync(ProductUsage usage, CancellationToken cancellationToken = default);
    Task<ProductReturn> RecordReturnAsync(ProductReturn productReturn, CancellationToken cancellationToken = default);
}

public sealed class InventoryTransactionService(ApplicationDbContext dbContext, ILogger<InventoryTransactionService> logger) : IInventoryTransactionService
{
    public async Task<IReadOnlyList<StoreProductLot>> GetStockLotsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.StoreProductLots
            .AsNoTracking()
            .Include(lot => lot.Product)
            .ThenInclude(product => product.Category)
            .OrderBy(lot => lot.ArrivalDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductUsage>> GetUsageHistoryAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ProductUsages
            .AsNoTracking()
            .Include(usage => usage.Product)
            .OrderByDescending(usage => usage.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductReturn>> GetReturnHistoryAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ProductReturns
            .AsNoTracking()
            .Include(productReturn => productReturn.Product)
            .Include(productReturn => productReturn.ProductUsage)
            .OrderByDescending(productReturn => productReturn.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductUsage>> GetUsagesForProductAsync(int? productId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.ProductUsages
            .AsNoTracking()
            .Include(usage => usage.Product)
            .AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(usage => usage.ProductId == productId.Value);
        }

        return await query
            .OrderByDescending(usage => usage.Date)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task<StoreProductLot> AddStockAsync(StoreProductLot lot, CancellationToken cancellationToken = default)
    {
        try
        {
            if (lot.QuantityAvailable <= 0)
            {
                throw new InvalidOperationException("Stock quantity must be greater than zero.");
            }

            var product = await dbContext.Products.FirstOrDefaultAsync(x => x.ItemId == lot.ProductId, cancellationToken)
                ?? throw new InvalidOperationException("Selected product was not found.");

            if (!product.IsActive)
            {
                throw new InvalidOperationException("Stock can only be added to active products.");
            }

            lot.AdditionType = "New";
            dbContext.StoreProductLots.Add(lot);
            await dbContext.SaveChangesAsync(cancellationToken);
            return lot;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error while adding stock for product {ProductId}", lot.ProductId);
            throw new InvalidOperationException("Unable to add stock right now.", exception);
        }
    }

    public async Task<ProductUsage> RecordUsageAsync(ProductUsage usage, CancellationToken cancellationToken = default)
    {
        try
        {
            var quantityToIssue = usage.Issued ?? 0;
            if (quantityToIssue <= 0)
            {
                throw new InvalidOperationException("Issued quantity must be greater than zero.");
            }

            var product = await dbContext.Products.FirstOrDefaultAsync(x => x.ItemId == usage.ProductId, cancellationToken)
                ?? throw new InvalidOperationException("Selected product was not found.");

            var lots = await dbContext.StoreProductLots
                .Where(lot => lot.ProductId == usage.ProductId && lot.QuantityAvailable > 0)
                .OrderBy(lot => lot.ArrivalDate)
                .ToListAsync(cancellationToken);

            var availableQuantity = lots.Sum(lot => lot.QuantityAvailable);
            if (availableQuantity < quantityToIssue)
            {
                throw new InvalidOperationException($"Insufficient stock. Available quantity for {product.Name} is {availableQuantity:0.##}.");
            }

            var remaining = quantityToIssue;
            foreach (var lot in lots)
            {
                if (remaining <= 0)
                {
                    break;
                }

                var deducted = Math.Min(lot.QuantityAvailable, remaining);
                lot.QuantityAvailable -= deducted;
                remaining -= deducted;
            }

            dbContext.ProductUsages.Add(usage);
            await dbContext.SaveChangesAsync(cancellationToken);
            return usage;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error while recording usage for product {ProductId}", usage.ProductId);
            throw new InvalidOperationException("Unable to record product usage right now.", exception);
        }
    }

    public async Task<ProductReturn> RecordReturnAsync(ProductReturn productReturn, CancellationToken cancellationToken = default)
    {
        try
        {
            if (productReturn.QuantityReturned <= 0)
            {
                throw new InvalidOperationException("Returned quantity must be greater than zero.");
            }

            var product = await dbContext.Products.FirstOrDefaultAsync(x => x.ItemId == productReturn.ProductId, cancellationToken)
                ?? throw new InvalidOperationException("Selected product was not found.");

            if (productReturn.UsageId.HasValue)
            {
                var usage = await dbContext.ProductUsages.FirstOrDefaultAsync(x => x.UsageId == productReturn.UsageId.Value, cancellationToken)
                    ?? throw new InvalidOperationException("Selected usage transaction was not found.");

                if (usage.ProductId != productReturn.ProductId)
                {
                    throw new InvalidOperationException("The selected usage does not belong to the selected product.");
                }

                var previouslyReturned = await dbContext.ProductReturns
                    .Where(x => x.UsageId == productReturn.UsageId.Value)
                    .SumAsync(x => x.QuantityReturned, cancellationToken);

                if ((usage.Issued ?? 0) < previouslyReturned + productReturn.QuantityReturned)
                {
                    throw new InvalidOperationException("Returned quantity cannot exceed the issued quantity.");
                }
            }

            dbContext.ProductReturns.Add(productReturn);
            dbContext.StoreProductLots.Add(new StoreProductLot
            {
                ProductId = productReturn.ProductId,
                QuantityAvailable = productReturn.QuantityReturned,
                ArrivalDate = productReturn.Date,
                AdditionType = "Return",
                UsageId = productReturn.UsageId
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            return productReturn;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error while recording return for product {ProductId}", productReturn.ProductId);
            throw new InvalidOperationException("Unable to record product return right now.", exception);
        }
    }
}
