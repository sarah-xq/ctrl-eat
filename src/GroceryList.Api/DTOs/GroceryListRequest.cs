namespace GroceryList.Api.DTOs;

public sealed class GroceryListRequest
{
    public IReadOnlyCollection<int>? IngredientIds { get; init; }
    public DateTime Date { get; init; }
}
