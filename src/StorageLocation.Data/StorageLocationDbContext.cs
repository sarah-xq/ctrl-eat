using Microsoft.EntityFrameworkCore;
using StorageLocation.Data.Entities;

namespace StorageLocation.Data;

public sealed class StorageLocationDbContext(DbContextOptions<StorageLocationDbContext> options) : DbContext(options)
{
    public DbSet<StorageLocationRecord> StorageLocations => Set<StorageLocationRecord>();

    public DbSet<StorageLocationIngredientRecord> StorageLocationIngredients => Set<StorageLocationIngredientRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StorageLocationDbContext).Assembly);
    }
}
