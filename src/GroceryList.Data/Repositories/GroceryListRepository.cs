using Microsoft.EntityFrameworkCore;
using GroceryList.Data.Entities;
using DomainGroceryList = GroceryList.Domain.Entities.GroceryList;

namespace GroceryList.Data.Repositories;

public sealed class GroceryListRepository(GroceryListDbContext dbContext) : IGroceryListRepository
{
    public async Task<IReadOnlyList<DomainGroceryList>> GetAllAsync(
        DateTime? date = null,
        CancellationToken cancellationToken = default)
    {
        var query = QueryGroceryLists();
        if (date.HasValue)
        {
            var normalizedDate = date.Value.Date;
            query = query.Where(record => record.Date == normalizedDate);
        }

        var records = await query
            .OrderBy(record => record.Id)
            .ToListAsync(cancellationToken);
        return records.Select(ToDomain).ToList();
    }

    public async Task<DomainGroceryList?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var record = await QueryGroceryLists()
            .SingleOrDefaultAsync(list => list.Id == id, cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async Task<DomainGroceryList> CreateAsync(
        DomainGroceryList groceryList,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(groceryList);

        var record = ToRecord(groceryList);
        dbContext.GroceryLists.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        groceryList.Id = record.Id;
        return groceryList;
    }

    public async Task<bool> UpdateAsync(
        DomainGroceryList groceryList,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(groceryList);

        var record = await dbContext.GroceryLists
            .Include(existing => existing.IngredientReferences)
            .SingleOrDefaultAsync(existing => existing.Id == groceryList.Id, cancellationToken);
        if (record is null)
        {
            return false;
        }

        record.Date = groceryList.Date;
        record.UpdatedAt = groceryList.UpdatedAt;
        dbContext.GroceryListIngredients.RemoveRange(record.IngredientReferences);
        record.IngredientReferences = groceryList.IngredientIds
            .Select((ingredientId, position) => new GroceryListIngredientRecord
            {
                GroceryListId = record.Id,
                IngredientId = ingredientId,
                Position = position
            })
            .ToList();

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.GroceryLists
            .SingleOrDefaultAsync(list => list.Id == id, cancellationToken);
        if (record is null)
        {
            return false;
        }

        dbContext.GroceryLists.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<GroceryListRecord> QueryGroceryLists() => dbContext.GroceryLists
        .AsNoTracking()
        .Include(list => list.IngredientReferences);

    private static GroceryListRecord ToRecord(DomainGroceryList groceryList)
    {
        var record = new GroceryListRecord
        {
            Date = groceryList.Date.Date,
            CreatedAt = groceryList.CreatedAt,
            UpdatedAt = groceryList.UpdatedAt
        };

        record.IngredientReferences = groceryList.IngredientIds
            .Select((ingredientId, position) => new GroceryListIngredientRecord
            {
                IngredientId = ingredientId,
                Position = position
            })
            .ToList();

        return record;
    }

    private static DomainGroceryList ToDomain(GroceryListRecord record) => DomainGroceryList.Rehydrate(
        record.Id,
        record.Date,
        record.IngredientReferences.OrderBy(reference => reference.Position).Select(reference => reference.IngredientId),
        record.CreatedAt,
        record.UpdatedAt);
}