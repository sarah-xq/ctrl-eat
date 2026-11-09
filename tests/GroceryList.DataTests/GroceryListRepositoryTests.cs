using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using GroceryList.Data;
using GroceryList.Data.Repositories;
using DomainGroceryList = GroceryList.Domain.Entities.GroceryList;

namespace GroceryList.DataTests;

public sealed class GroceryListRepositoryTests
{
    [Fact]
    public async Task MigrationCreatesExpectedTablesAndOnlyLocalForeignKey()
    {
        await using var database = await TemporaryGroceryListDatabase.CreateAsync();
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

        Assert.Contains("GroceryLists", tables);
        Assert.Contains("GroceryListIngredients", tables);
        Assert.DoesNotContain("Ingredients", tables);

        await using var foreignKeyCommand = connection.CreateCommand();
        foreignKeyCommand.CommandText = "PRAGMA foreign_key_list('GroceryListIngredients');";
        await using var foreignKeyReader = await foreignKeyCommand.ExecuteReaderAsync();
        var foreignKeyTables = new List<string>();
        while (await foreignKeyReader.ReadAsync())
        {
            foreignKeyTables.Add(foreignKeyReader.GetString(2));
        }

        Assert.Equal(new[] { "GroceryLists" }, foreignKeyTables);
    }

    [Fact]
    public async Task CreateAndRetrievePersistsIngredientOrderAndTimestampsAcrossContexts()
    {
        await using var database = await TemporaryGroceryListDatabase.CreateAsync();
        var date = new DateTime(2026, 11, 12, 14, 30, 0, DateTimeKind.Utc);
        var groceryList = new DomainGroceryList(date, [7, 3]);
        var createdAt = groceryList.CreatedAt;
        var updatedAt = groceryList.UpdatedAt;

        await using (var createContext = database.CreateContext())
        {
            var repository = new GroceryListRepository(createContext);
            var created = await repository.CreateAsync(groceryList);
            Assert.True(created.Id > 0);
        }

        await using var readContext = database.CreateContext();
        var readRepository = new GroceryListRepository(readContext);
        var persisted = Assert.IsType<DomainGroceryList>(await readRepository.GetByIdAsync(groceryList.Id));

        Assert.Equal(date.Date, persisted.Date);
        Assert.Equal(new[] { 7, 3 }, persisted.IngredientIds);
        Assert.Equal(createdAt, persisted.CreatedAt);
        Assert.Equal(updatedAt, persisted.UpdatedAt);
        Assert.Equal(DateTimeKind.Utc, persisted.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, persisted.UpdatedAt.Kind);
    }

    [Fact]
    public async Task GetAllAsyncFiltersByDateIgnoringTimeOfDay()
    {
        await using var database = await TemporaryGroceryListDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var repository = new GroceryListRepository(context);
        var wantedDate = new DateTime(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc);
        var first = await repository.CreateAsync(new DomainGroceryList(wantedDate.AddHours(8)));
        var second = await repository.CreateAsync(new DomainGroceryList(wantedDate.AddHours(21)));
        await repository.CreateAsync(new DomainGroceryList(wantedDate.AddDays(1)));

        var results = await repository.GetAllAsync(wantedDate.AddHours(17));

        Assert.Equal(new[] { first.Id, second.Id }, results.Select(list => list.Id));
        Assert.All(results, list => Assert.Equal(wantedDate.Date, list.Date));
        Assert.Equal(3, (await repository.GetAllAsync()).Count);
    }

    [Fact]
    public async Task UpdatePreservesCreatedAtAndPersistsUpdatedDateIngredientsAndTimestamp()
    {
        await using var database = await TemporaryGroceryListDatabase.CreateAsync();
        DomainGroceryList created;
        var createdAt = DateTime.UtcNow;
        DateTime previousUpdatedAt;
        await using (var context = database.CreateContext())
        {
            var repository = new GroceryListRepository(context);
            created = await repository.CreateAsync(new DomainGroceryList(
                new DateTime(2026, 11, 12), [7, 3]));
            createdAt = created.CreatedAt;
            previousUpdatedAt = created.UpdatedAt;

            created.Update(new DateTime(2026, 11, 13, 18, 30, 0), [3, 9]);
            Assert.True(await repository.UpdateAsync(created));
        }

        await using var verifyContext = database.CreateContext();
        var updated = Assert.IsType<DomainGroceryList>(
            await new GroceryListRepository(verifyContext).GetByIdAsync(created.Id));

        Assert.Equal(new DateTime(2026, 11, 13), updated.Date);
        Assert.Equal(new[] { 3, 9 }, updated.IngredientIds);
        Assert.Equal(createdAt, updated.CreatedAt);
        Assert.True(updated.UpdatedAt >= previousUpdatedAt);
        Assert.True(updated.UpdatedAt >= created.UpdatedAt);
    }

    [Fact]
    public async Task EmptyIngredientCollectionPersists()
    {
        await using var database = await TemporaryGroceryListDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var repository = new GroceryListRepository(context);

        var created = await repository.CreateAsync(new DomainGroceryList(new DateTime(2026, 11, 12), []));
        var retrieved = await repository.GetByIdAsync(created.Id);

        Assert.NotNull(retrieved);
        Assert.Empty(retrieved.IngredientIds);
    }

    [Fact]
    public async Task DeleteRemovesListAndReturnsFalseForMissingId()
    {
        await using var database = await TemporaryGroceryListDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var repository = new GroceryListRepository(context);
        var created = await repository.CreateAsync(new DomainGroceryList(new DateTime(2026, 11, 12), [4]));

        Assert.True(await repository.DeleteAsync(created.Id));
        Assert.Null(await repository.GetByIdAsync(created.Id));
        Assert.False(await repository.DeleteAsync(created.Id));
    }

    [Fact]
    public async Task UpdateAsyncReturnsFalseForMissingIdWithoutChangingRecords()
    {
        await using var database = await TemporaryGroceryListDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var repository = new GroceryListRepository(context);
        var existing = await repository.CreateAsync(new DomainGroceryList(
            new DateTime(2026, 11, 12), [7, 3]));
        var missing = DomainGroceryList.Rehydrate(
            existing.Id + 1000,
            new DateTime(2026, 11, 13),
            [99],
            DateTime.UtcNow.AddDays(-2),
            DateTime.UtcNow.AddDays(-1));

        Assert.False(await repository.UpdateAsync(missing));

        var persisted = Assert.Single(await repository.GetAllAsync());
        Assert.Equal(existing.Id, persisted.Id);
        Assert.Equal(new DateTime(2026, 11, 12), persisted.Date);
        Assert.Equal(new[] { 7, 3 }, persisted.IngredientIds);
        Assert.Equal(existing.CreatedAt, persisted.CreatedAt);
        Assert.Equal(existing.UpdatedAt, persisted.UpdatedAt);
        Assert.Null(await repository.GetByIdAsync(missing.Id));
    }

    private sealed class TemporaryGroceryListDatabase : IAsyncDisposable
    {
        private readonly string _databasePath;

        private TemporaryGroceryListDatabase(string databasePath)
        {
            _databasePath = databasePath;
        }

        public GroceryListDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<GroceryListDbContext>()
                .UseSqlite($"Data Source={_databasePath}")
                .Options;
            return new GroceryListDbContext(options);
        }

        public static async Task<TemporaryGroceryListDatabase> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"ctrl-eat-grocery-{Guid.NewGuid():N}.db");
            var database = new TemporaryGroceryListDatabase(path);
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
}