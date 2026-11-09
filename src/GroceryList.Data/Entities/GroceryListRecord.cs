namespace GroceryList.Data.Entities;

public sealed class GroceryListRecord
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<GroceryListIngredientRecord> IngredientReferences { get; set; } = new List<GroceryListIngredientRecord>();
}