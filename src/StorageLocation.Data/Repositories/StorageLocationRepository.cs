using Microsoft.EntityFrameworkCore;
using StorageLocation.Data.Entities;
using DomainStorageLocation = StorageLocation.Domain.Entities.StorageLocation;

namespace StorageLocation.Data.Repositories;

public sealed class StorageLocationRepository(StorageLocationDbContext dbContext) : IStorageLocationRepository
{
    public async Task<IReadOnlyList<DomainStorageLocation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var records = await QueryLocations()
            .OrderBy(location => location.Id)
            .ToListAsync(cancellationToken);
        return records.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<DomainStorageLocation>> FindAsync(
        string? name = null,
        string? type = null,
        CancellationToken cancellationToken = default)
    {
        var query = QueryLocations();
        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(location => EF.Functions.Like(
                location.Name,
                $"%{EscapeLikePattern(name)}%",
                "\\"));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(location => EF.Functions.Like(
                location.Type,
                EscapeLikePattern(type),
                "\\"));
        }

        var records = await query.OrderBy(location => location.Id).ToListAsync(cancellationToken);
        return records.Select(ToDomain).ToList();
    }

    public async Task<DomainStorageLocation?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var record = await QueryLocations()
            .SingleOrDefaultAsync(location => location.Id == id, cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async Task<DomainStorageLocation> CreateAsync(
        DomainStorageLocation location,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        var record = ToRecord(location);
        dbContext.StorageLocations.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        location.Id = record.Id;
        return location;
    }

    public async Task<bool> UpdateAsync(
        DomainStorageLocation location,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        var record = await dbContext.StorageLocations
            .Include(existing => existing.IngredientReferences)
            .SingleOrDefaultAsync(existing => existing.Id == location.Id, cancellationToken);
        if (record is null)
        {
            return false;
        }

        record.Name = location.Name;
        record.Type = location.Type;
        record.MinimumTemperature = location.MinimumTemperature;
        record.MaximumTemperature = location.MaximumTemperature;
        record.CreatedAt = location.CreatedAt;
        record.UpdatedAt = location.UpdatedAt;

        var ingredientIds = location.IngredientIds.ToHashSet();
        foreach (var oldReference in record.IngredientReferences
                     .Where(reference => !ingredientIds.Contains(reference.IngredientId))
                     .ToArray())
        {
            record.IngredientReferences.Remove(oldReference);
            dbContext.StorageLocationIngredients.Remove(oldReference);
        }

        var existingById = record.IngredientReferences.ToDictionary(reference => reference.IngredientId);
        for (var position = 0; position < location.IngredientIds.Count; position++)
        {
            var ingredientId = location.IngredientIds[position];
            if (existingById.TryGetValue(ingredientId, out var reference))
            {
                reference.Position = position;
            }
            else
            {
                record.IngredientReferences.Add(new StorageLocationIngredientRecord
                {
                    StorageLocationId = record.Id,
                    IngredientId = ingredientId,
                    Position = position
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.StorageLocations
            .SingleOrDefaultAsync(location => location.Id == id, cancellationToken);
        if (record is null)
        {
            return false;
        }

        dbContext.StorageLocations.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<StorageLocationRecord> QueryLocations() => dbContext.StorageLocations
        .AsNoTracking()
        .Include(location => location.IngredientReferences);

    private static StorageLocationRecord ToRecord(DomainStorageLocation location)
    {
        var record = new StorageLocationRecord
        {
            Name = location.Name,
            Type = location.Type,
            MinimumTemperature = location.MinimumTemperature,
            MaximumTemperature = location.MaximumTemperature,
            CreatedAt = location.CreatedAt,
            UpdatedAt = location.UpdatedAt
        };

        for (var position = 0; position < location.IngredientIds.Count; position++)
        {
            record.IngredientReferences.Add(new StorageLocationIngredientRecord
            {
                IngredientId = location.IngredientIds[position],
                Position = position
            });
        }

        return record;
    }

    private static DomainStorageLocation ToDomain(StorageLocationRecord record) => DomainStorageLocation.Rehydrate(
        record.Id,
        record.Name,
        record.Type,
        record.MinimumTemperature,
        record.MaximumTemperature,
        record.IngredientReferences.OrderBy(reference => reference.Position).Select(reference => reference.IngredientId),
        record.CreatedAt,
        record.UpdatedAt);

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);
}
