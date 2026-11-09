namespace CtrlEat.RazorClient.Models;

public class StorageLocationRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal MinimumTemperature { get; set; }
    public decimal MaximumTemperature { get; set; }
    public List<int> IngredientIds { get; set; } = [];
}

public sealed class StorageLocationResponse : StorageLocationRequest
{
    public int Id { get; init; }
    public List<StoredIngredient> Ingredients { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}

public sealed record StoredIngredient(int Id, string Name, string FoodGroup, decimal Calories);