using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StorageLocation.Data;
using StorageLocation.Data.Entities;
using StorageLocation.Data.Repositories;
using DomainStorageLocation = StorageLocation.Domain.Entities.StorageLocation;

namespace StorageLocation.DataTests;

public sealed class StorageLocationRepositoryTests
{
    [Fact]
    public async Task RepositoryPersistsCrudIngredientsAndTimestampsAcrossContexts()
    {
        await using var database = await TemporaryStorageLocationDatabase.CreateAsync();
        var location = new DomainStorageLocation("Main fridge", "Cold", 1.25m, 5.75m, [7, 3]);
        var createdAt = location.CreatedAt;
        var updatedAt = location.UpdatedAt;

        await using (var createContext = database.CreateContext())
        {
            var repository = new StorageLocationRepository(createContext);
            var created = await repository.CreateAsync(location);
            Assert.True(created.Id > 0);
        }

        DomainStorageLocation persisted;
        await using (var readContext = database.CreateContext())
        {
            var repository = new StorageLocationRepository(readContext);
            persisted = Assert.IsType<DomainStorageLocation>(await repository.GetByIdAsync(location.Id));
            Assert.Equal("Main fridge", persisted.Name);
            Assert.Equal("Cold", persisted.Type);
            Assert.Equal(1.25m, persisted.MinimumTemperature);
            Assert.Equal(5.75m, persisted.MaximumTemperature);
            Assert.Equal(new[] { 7, 3 }, persisted.IngredientIds);
            Assert.Equal(createdAt, persisted.CreatedAt);
            Assert.Equal(updatedAt, persisted.UpdatedAt);
            Assert.Equal(DateTimeKind.Utc, persisted.CreatedAt.Kind);
            Assert.Equal(DateTimeKind.Utc, persisted.UpdatedAt.Kind);

            persisted.Update("Updated fridge", "Chilled", 2.5m, 6.5m, [3, 9]);
            Assert.True(await repository.UpdateAsync(persisted));
        }

        await using (var verifyContext = database.CreateContext())
        {
            var repository = new StorageLocationRepository(verifyContext);
            var updated = Assert.IsType<DomainStorageLocation>(await repository.GetByIdAsync(location.Id));
            Assert.Equal("Updated fridge", updated.Name);
            Assert.Equal("Chilled", updated.Type);
            Assert.Equal(2.5m, updated.MinimumTemperature);
            Assert.Equal(6.5m, updated.MaximumTemperature);
            Assert.Equal(new[] { 3, 9 }, updated.IngredientIds);
            Assert.Equal(createdAt, updated.CreatedAt);
            Assert.True(updated.UpdatedAt >= updatedAt);

            Assert.True(await repository.DeleteAsync(location.Id));
            Assert.Null(await repository.GetByIdAsync(location.Id));
            Assert.False(await repository.DeleteAsync(location.Id));
        }
    }

    [Fact]
    public async Task RepositoryFiltersByNameAndType()
    {
        await using var database = await TemporaryStorageLocationDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var repository = new StorageLocationRepository(context);

        await repository.CreateAsync(new DomainStorageLocation("Main fridge", "Cold", 1, 5));
        await repository.CreateAsync(new DomainStorageLocation("Chest freezer", "Frozen", -20, -10));
        await repository.CreateAsync(new DomainStorageLocation("Pantry", "Dry", 10, 25));

        var byName = await repository.FindAsync(name: "FRIDge");
        var byType = await repository.FindAsync(type: "fRoZeN");
        var combined = await repository.FindAsync(name: "freezer", type: "frozen");

        Assert.Equal("Main fridge", Assert.Single(byName).Name);
        Assert.Equal("Chest freezer", Assert.Single(byType).Name);
        Assert.Equal("Chest freezer", Assert.Single(combined).Name);
        Assert.Equal(3, (await repository.GetAllAsync()).Count);
    }

    [Fact]
    public async Task DatabaseRejectsDuplicateIngredientIdForOneLocation()
    {
        await using var database = await TemporaryStorageLocationDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var location = new StorageLocationRecord
        {
            Name = "Main fridge",
            Type = "Cold",
            MinimumTemperature = 1,
            MaximumTemperature = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.StorageLocations.Add(location);
        await context.SaveChangesAsync();

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "StorageLocationIngredients" ("StorageLocationId", "IngredientId", "Position")
            VALUES ({location.Id}, {12}, {0});
            """);

        var exception = await Assert.ThrowsAsync<SqliteException>(() => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "StorageLocationIngredients" ("StorageLocationId", "IngredientId", "Position")
            VALUES ({location.Id}, {12}, {1});
            """));
        Assert.Equal(19, exception.SqliteErrorCode);
    }

    [Fact]
    public async Task MigrationCreatesExpectedTablesAndOnlyLocalForeignKey()
    {
        await using var database = await TemporaryStorageLocationDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var tableCommand = connection.CreateCommand();
        tableCommand.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name;";
        var tables = new List<string>();
        await using (var reader = await tableCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        Assert.Contains("StorageLocations", tables);
        Assert.Contains("StorageLocationIngredients", tables);
        Assert.DoesNotContain("Profiles", tables);
        Assert.DoesNotContain("Ingredients", tables);

        await using var foreignKeyCommand = connection.CreateCommand();
        foreignKeyCommand.CommandText = "PRAGMA foreign_key_list('StorageLocationIngredients');";
        await using var foreignKeyReader = await foreignKeyCommand.ExecuteReaderAsync();
        var foreignKeyTables = new List<string>();
        while (await foreignKeyReader.ReadAsync())
        {
            foreignKeyTables.Add(foreignKeyReader.GetString(2));
        }

        Assert.Equal(new[] { "StorageLocations" }, foreignKeyTables);
    }

    private sealed class TemporaryStorageLocationDatabase : IAsyncDisposable
    {
        private readonly string _databasePath;

        private TemporaryStorageLocationDatabase(string databasePath)
        {
            _databasePath = databasePath;
        }

        public StorageLocationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<StorageLocationDbContext>()
                .UseSqlite($"Data Source={_databasePath}")
                .Options;
            return new StorageLocationDbContext(options);
        }

        public static async Task<TemporaryStorageLocationDatabase> CreateAsync()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"ctrl-eat-storage-{Guid.NewGuid():N}.db");
            var database = new TemporaryStorageLocationDatabase(databasePath);
            await using var context = database.CreateContext();
            await context.Database.MigrateAsync();
            return database;
        }

        public ValueTask DisposeAsync()
        {
            SqliteConnection.ClearAllPools();
            foreach (var suffix in new[] { string.Empty, "-shm", "-wal" })
            {
                var path = _databasePath + suffix;
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task UpdateAsyncReturnsFalseForMissingIdWithoutChangingRecords()
    {
        await using var database = await TemporaryStorageLocationDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var repository = new StorageLocationRepository(context);
        var existing = await repository.CreateAsync(
            new DomainStorageLocation("Main fridge", "Cold", 1, 5, [7, 3]));
        var originalCreatedAt = existing.CreatedAt;
        var originalUpdatedAt = existing.UpdatedAt;
        var missing = DomainStorageLocation.Rehydrate(
            existing.Id + 1000,
            "Missing location",
            "Dry",
            10,
            20,
            [99],
            DateTime.UtcNow.AddDays(-2),
            DateTime.UtcNow.AddDays(-1));

        Assert.False(await repository.UpdateAsync(missing));

        var locations = await repository.GetAllAsync();
        var persisted = Assert.Single(locations);
        Assert.Equal(existing.Id, persisted.Id);
        Assert.Equal("Main fridge", persisted.Name);
        Assert.Equal("Cold", persisted.Type);
        Assert.Equal(1, persisted.MinimumTemperature);
        Assert.Equal(5, persisted.MaximumTemperature);
        Assert.Equal(new[] { 7, 3 }, persisted.IngredientIds);
        Assert.Equal(originalCreatedAt, persisted.CreatedAt);
        Assert.Equal(originalUpdatedAt, persisted.UpdatedAt);
        Assert.Null(await repository.GetByIdAsync(missing.Id));
    }
}
