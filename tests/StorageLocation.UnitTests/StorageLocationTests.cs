using StorageLocationEntity = StorageLocation.Domain.Entities.StorageLocation;

namespace StorageLocation.UnitTests;

public sealed class StorageLocationTests
{
    [Fact]
    public void ConstructorTrimsNameAndTypeAndPreservesValues()
    {
        var location = new StorageLocationEntity("  Main fridge  ", "  Cold  ", 1, 5, [3, 8]);

        Assert.Equal("Main fridge", location.Name);
        Assert.Equal("Cold", location.Type);
        Assert.Equal(1, location.MinimumTemperature);
        Assert.Equal(5, location.MaximumTemperature);
        Assert.Equal(new[] { 3, 8 }, location.IngredientIds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void ConstructorRejectsMissingName(string? name)
    {
        Assert.Throws<ArgumentException>(() => new StorageLocationEntity(name, "Cold", 1, 5));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void ConstructorRejectsMissingType(string? type)
    {
        Assert.Throws<ArgumentException>(() => new StorageLocationEntity("Main fridge", type, 1, 5));
    }

    [Fact]
    public void ConstructorRejectsMinimumTemperatureAboveMaximum()
    {
        Assert.Throws<ArgumentException>(() => new StorageLocationEntity("Main fridge", "Cold", 5, 4));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ConstructorRejectsNonPositiveIngredientIds(int ingredientId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new StorageLocationEntity("Main fridge", "Cold", 1, 5, [ingredientId]));
    }

    [Fact]
    public void ConstructorRemovesDuplicateIngredientIdsAndPreservesFirstOccurrenceOrder()
    {
        var location = new StorageLocationEntity("Main fridge", "Cold", 1, 5, [7, 3, 7, 2, 3]);

        Assert.Equal(new[] { 7, 3, 2 }, location.IngredientIds);
    }

    [Fact]
    public void NullIngredientIdsAreTreatedAsEmpty()
    {
        var location = new StorageLocationEntity("Main fridge", "Cold", 1, 5, null);

        Assert.Empty(location.IngredientIds);
    }

    [Fact]
    public void ConstructorInitializesUtcTimestamps()
    {
        var beforeConstruction = DateTime.UtcNow;
        var location = new StorageLocationEntity("Main fridge", "Cold", 1, 5);
        var afterConstruction = DateTime.UtcNow;

        Assert.InRange(location.CreatedAt, beforeConstruction, afterConstruction);
        Assert.InRange(location.UpdatedAt, beforeConstruction, afterConstruction);
        Assert.Equal(DateTimeKind.Utc, location.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, location.UpdatedAt.Kind);
    }

    [Fact]
    public void UpdateChangesValuesAndIngredientsButPreservesCreatedAt()
    {
        var location = new StorageLocationEntity("Main fridge", "Cold", 1, 5, [7]);
        var createdAt = location.CreatedAt;
        var previousUpdatedAt = location.UpdatedAt;
        var beforeUpdate = DateTime.UtcNow;

        location.Update("  Pantry  ", "  Dry  ", 10, 25, [4, 4, 6]);

        Assert.Equal("Pantry", location.Name);
        Assert.Equal("Dry", location.Type);
        Assert.Equal(10, location.MinimumTemperature);
        Assert.Equal(25, location.MaximumTemperature);
        Assert.Equal(new[] { 4, 6 }, location.IngredientIds);
        Assert.Equal(createdAt, location.CreatedAt);
        Assert.InRange(location.UpdatedAt, previousUpdatedAt, DateTime.UtcNow);
        Assert.True(location.UpdatedAt >= beforeUpdate);
    }

    [Fact]
    public void InvalidUpdateDoesNotChangeEntity()
    {
        var location = new StorageLocationEntity("Main fridge", "Cold", 1, 5, [7]);
        var createdAt = location.CreatedAt;
        var updatedAt = location.UpdatedAt;

        Assert.Throws<ArgumentException>(() => location.Update("Pantry", "Dry", 10, 5, [4]));

        Assert.Equal("Main fridge", location.Name);
        Assert.Equal("Cold", location.Type);
        Assert.Equal(1, location.MinimumTemperature);
        Assert.Equal(5, location.MaximumTemperature);
        Assert.Equal(new[] { 7 }, location.IngredientIds);
        Assert.Equal(createdAt, location.CreatedAt);
        Assert.Equal(updatedAt, location.UpdatedAt);
    }

    [Fact]
    public void RehydratePreservesPersistedValuesAndTimestamps()
    {
        var createdAt = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(1234);
        var updatedAt = new DateTime(2025, 2, 3, 4, 5, 6, DateTimeKind.Utc).AddTicks(5678);

        var location = StorageLocationEntity.Rehydrate(
            42, "Main fridge", "Cold", 1.25m, 5.75m, [7, 3], createdAt, updatedAt);

        Assert.Equal(42, location.Id);
        Assert.Equal("Main fridge", location.Name);
        Assert.Equal("Cold", location.Type);
        Assert.Equal(1.25m, location.MinimumTemperature);
        Assert.Equal(5.75m, location.MaximumTemperature);
        Assert.Equal(new[] { 7, 3 }, location.IngredientIds);
        Assert.Equal(createdAt, location.CreatedAt);
        Assert.Equal(updatedAt, location.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RehydrateRejectsNonPositivePersistedIds(int id)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => StorageLocationEntity.Rehydrate(
            id, "Main fridge", "Cold", 1, 5, [], DateTime.UtcNow, DateTime.UtcNow));
    }

    [Theory]
    [InlineData(" ", "Cold", 1, 5, 2)]
    [InlineData("Main fridge", " ", 1, 5, 2)]
    [InlineData("  Main fridge  ", "Cold", 1, 5, 2)]
    [InlineData("Main fridge", "  Cold  ", 1, 5, 2)]
    [InlineData("Main fridge", "Cold", 5, 4, 2)]
    [InlineData("Main fridge", "Cold", 1, 5, 0)]
    public void RehydrateRejectsInvalidPersistedValues(
        string name,
        string type,
        decimal minimumTemperature,
        decimal maximumTemperature,
        int ingredientId)
    {
        Assert.ThrowsAny<ArgumentException>(() => StorageLocationEntity.Rehydrate(
            42, name, type, minimumTemperature, maximumTemperature, [ingredientId], DateTime.UtcNow, DateTime.UtcNow));
    }
}
