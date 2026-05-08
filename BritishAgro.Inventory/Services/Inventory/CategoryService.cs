using BritishAgro.Inventory.Data;
using Microsoft.EntityFrameworkCore;

namespace BritishAgro.Inventory.Services.Inventory;

public interface ICategoryService
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Category> SaveAsync(Category category, CancellationToken cancellationToken = default);
    Task DeleteAsync(int categoryId, CancellationToken cancellationToken = default);
}

public sealed class CategoryService(ApplicationDbContext dbContext, ILogger<CategoryService> logger) : ICategoryService
{
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .Include(category => category.Products)
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Category> SaveAsync(Category category, CancellationToken cancellationToken = default)
    {
        try
        {
            if (category.CategoryId == 0)
            {
                dbContext.Categories.Add(category);
            }
            else
            {
                var existing = await dbContext.Categories.FirstOrDefaultAsync(x => x.CategoryId == category.CategoryId, cancellationToken)
                    ?? throw new InvalidOperationException("Category not found.");

                existing.Name = category.Name.Trim();
                existing.Description = category.Description?.Trim();
                existing.IsActive = category.IsActive;
                category = existing;
            }

            if (category.CategoryId == 0)
            {
                category.Name = category.Name.Trim();
                category.Description = category.Description?.Trim();
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return category;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error while saving category {CategoryName}", category.Name);
            throw new InvalidOperationException("Unable to save the category right now.", exception);
        }
    }

    public async Task DeleteAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await dbContext.Categories
                .Include(x => x.Products)
                .FirstOrDefaultAsync(x => x.CategoryId == categoryId, cancellationToken)
                ?? throw new InvalidOperationException("Category not found.");

            if (category.Products.Count > 0)
            {
                throw new InvalidOperationException("This category cannot be deleted because products are linked to it.");
            }

            dbContext.Categories.Remove(category);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error while deleting category {CategoryId}", categoryId);
            throw new InvalidOperationException("Unable to delete the category right now.", exception);
        }
    }
}
