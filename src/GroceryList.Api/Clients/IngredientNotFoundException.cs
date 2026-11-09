namespace GroceryList.Api.Clients;

public sealed class IngredientNotFoundException(int id)
    : Exception($"Ingredient with id {id} was not found.")
{
    public int IngredientId { get; } = id;
}
