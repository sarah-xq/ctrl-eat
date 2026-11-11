namespace GroceryList.Api.DTOs;

public sealed record GroceryListResponse(
    int Id,
    IReadOnlyList<StoredIngredientResponse> Ingredients,
    DateTime Date,
    DateTime CreatedAt);
