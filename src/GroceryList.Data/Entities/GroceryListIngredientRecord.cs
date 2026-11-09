namespace GroceryList.Data.Entities;

public sealed class GroceryListIngredientRecord
{
    public int GroceryListId { get; set; }
    public int IngredientId { get; set; }
    public int Position { get; set; }
}