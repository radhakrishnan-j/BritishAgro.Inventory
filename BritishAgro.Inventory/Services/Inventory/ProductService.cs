using BritishAgro.Inventory.Data;
using Microsoft.EntityFrameworkCore;

namespace BritishAgro.Inventory.Services.Inventory;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<Product> SaveAsync(Product product, CancellationToken cancellationToken = default);
    Task DeleteAsync(int productId, CancellationToken cancellationToken = default);
}

public sealed class ProductService(ApplicationDbContext dbContext, ILogger<ProductService> logger) : IProductService
{
    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.StoreStocks)
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Product> SaveAsync(Product product, CancellationToken cancellationToken = default)
    {
        try
        {
            if (product.CategoryId.HasValue)
            {
                var categoryExists = await dbContext.Categories.AnyAsync(x => x.CategoryId == product.CategoryId.Value, cancellationToken);
                if (!categoryExists)
                {
                    throw new InvalidOperationException("Selected category does not exist.");
                }
            }

            if (product.ItemId == 0)
            {
                product.Name = product.Name.Trim();
                product.Description = product.Description?.Trim();
                product.UnitOfMeasurement = product.UnitOfMeasurement?.Trim();
                dbContext.Products.Add(product);
            }
            else
            {
                var existing = await dbContext.Products.FirstOrDefaultAsync(x => x.ItemId == product.ItemId, cancellationToken)
                    ?? throw new InvalidOperationException("Product not found.");

                existing.Name = product.Name.Trim();
                existing.Description = product.Description?.Trim();
                existing.UnitOfMeasurement = product.UnitOfMeasurement?.Trim();
                existing.IsActive = product.IsActive;
                existing.CategoryId = product.CategoryId;
                existing.ReorderLevel = product.ReorderLevel;
                product = existing;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return product;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error while saving product {ProductName}", product.Name);
            throw new InvalidOperationException("Unable to save the product right now.", exception);
        }
    }

    public async Task DeleteAsync(int productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await dbContext.Products
                .Include(x => x.StoreStocks)
                .Include(x => x.ProductUsages)
                .Include(x => x.ProductReturns)
                .FirstOrDefaultAsync(x => x.ItemId == productId, cancellationToken)
                ?? throw new InvalidOperationException("Product not found.");

            if (product.StoreStocks.Count > 0 || product.ProductUsages.Count > 0 || product.ProductReturns.Count > 0)
            {
                throw new InvalidOperationException("This product cannot be deleted because inventory history exists for it.");
            }

            dbContext.Products.Remove(product);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error while deleting product {ProductId}", productId);
            throw new InvalidOperationException("Unable to delete the product right now.", exception);
        }
    }
}
