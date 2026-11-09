using GroceryListEntity = GroceryList.Domain.Entities.GroceryList;

namespace GroceryList.UnitTests;

public sealed class GroceryListTests
{
    [Fact]
    public void ConstructorStoresValuesAndNormalizesDate()
    {
        var date = new DateTime(2026, 11, 12, 14, 30, 0, DateTimeKind.Utc);

        var groceryList = new GroceryListEntity(date, [3, 8]);

        Assert.Equal(date.Date, groceryList.Date);
        Assert.Equal(new[] { 3, 8 }, groceryList.IngredientIds);
    }

    [Fact]
    public void ConstructorRejectsDefaultDate()
    {
        Assert.Throws<ArgumentException>(() => new GroceryListEntity(default));
    }

    [Fact]
    public void ConstructorPreservesDateKindWhenNormalizing()
    {
        var date = new DateTime(2026, 11, 12, 23, 45, 59, DateTimeKind.Local);

        var groceryList = new GroceryListEntity(date);

        Assert.Equal(date.Date, groceryList.Date);
        Assert.Equal(date.Kind, groceryList.Date.Kind);
    }

    [Fact]
    public void NullIngredientIdsAreTreatedAsEmpty()
    {
        var groceryList = new GroceryListEntity(new DateTime(2026, 11, 12), null);

        Assert.Empty(groceryList.IngredientIds);
    }

    [Fact]
    public void EmptyIngredientIdsAreAllowed()
    {
        var groceryList = new GroceryListEntity(new DateTime(2026, 11, 12), []);

        Assert.Empty(groceryList.IngredientIds);
    }

    [Fact]
    public void ConstructorRemovesDuplicatesAndPreservesOriginalOrder()
    {
        var groceryList = new GroceryListEntity(
            new DateTime(2026, 11, 12),
            [7, 3, 7, 2, 3]);

        Assert.Equal(new[] { 7, 3, 2 }, groceryList.IngredientIds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ConstructorRejectsNonpositiveIngredientIds(int ingredientId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new GroceryListEntity(new DateTime(2026, 11, 12), [ingredientId]));
    }

    [Fact]
    public void ConstructorInitializesUtcTimestamps()
    {
        var beforeConstruction = DateTime.UtcNow;

        var groceryList = new GroceryListEntity(new DateTime(2026, 11, 12));

        var afterConstruction = DateTime.UtcNow;

        Assert.InRange(groceryList.CreatedAt, beforeConstruction, afterConstruction);
        Assert.InRange(groceryList.UpdatedAt, beforeConstruction, afterConstruction);
        Assert.Equal(DateTimeKind.Utc, groceryList.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, groceryList.UpdatedAt.Kind);
    }

    [Fact]
    public void UpdateReplacesDateAndIngredientIdsPreservesCreatedAtAndUpdatesUpdatedAt()
    {
        var groceryList = new GroceryListEntity(new DateTime(2026, 11, 12), [7]);
        var createdAt = groceryList.CreatedAt;
        var previousUpdatedAt = groceryList.UpdatedAt;
        var beforeUpdate = DateTime.UtcNow;
        var newDate = new DateTime(2026, 11, 13, 18, 30, 0);

        groceryList.Update(newDate, [4, 4, 6]);

        Assert.Equal(newDate.Date, groceryList.Date);
        Assert.Equal(new[] { 4, 6 }, groceryList.IngredientIds);
        Assert.Equal(createdAt, groceryList.CreatedAt);
        Assert.InRange(groceryList.UpdatedAt, previousUpdatedAt, DateTime.UtcNow);
        Assert.True(groceryList.UpdatedAt >= beforeUpdate);
        Assert.Equal(DateTimeKind.Utc, groceryList.UpdatedAt.Kind);
    }

    [Fact]
    public void InvalidUpdateDoesNotPartiallyModifyEntity()
    {
        var groceryList = new GroceryListEntity(new DateTime(2026, 11, 12), [7]);
        var originalDate = groceryList.Date;
        var originalIngredientIds = groceryList.IngredientIds.ToArray();
        var createdAt = groceryList.CreatedAt;
        var updatedAt = groceryList.UpdatedAt;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => groceryList.Update(new DateTime(2026, 11, 13), [0]));

        Assert.Equal(originalDate, groceryList.Date);
        Assert.Equal(originalIngredientIds, groceryList.IngredientIds);
        Assert.Equal(createdAt, groceryList.CreatedAt);
        Assert.Equal(updatedAt, groceryList.UpdatedAt);
    }

    [Fact]
    public void RehydratePreservesPersistedValuesAndTimestamps()
    {
        var date = new DateTime(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc);
        var createdAt = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(1234);
        var updatedAt = new DateTime(2025, 2, 3, 4, 5, 6, DateTimeKind.Utc).AddTicks(5678);

        var groceryList = GroceryListEntity.Rehydrate(
            42,
            date,
            [7, 3],
            createdAt,
            updatedAt);

        Assert.Equal(42, groceryList.Id);
        Assert.Equal(date, groceryList.Date);
        Assert.Equal(new[] { 7, 3 }, groceryList.IngredientIds);
        Assert.Equal(createdAt, groceryList.CreatedAt);
        Assert.Equal(updatedAt, groceryList.UpdatedAt);
    }
}