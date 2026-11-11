namespace Recipe.Domain;

public sealed class Recipe
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Author { get; set; }
    public int TotalTimeMinutes { get; set; }
    public int CookTimeMinutes { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal TotalCalories { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<RecipeIngredient> Ingredients { get; set; } = new();
    public static void Validate(string? name, int totalTime, int cookTime, IEnumerable<(int id, decimal quantity)> ingredients)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Recipe name is required.");
        if (totalTime < 0 || cookTime < 0 || cookTime > totalTime) throw new ArgumentException("Invalid recipe timing.");
        var items = ingredients.ToList();
        if (items.Any(x => x.id <= 0 || x.quantity <= 0)) throw new ArgumentException("Ingredient IDs and quantities must be positive.");
        if (items.GroupBy(x => x.id).Any(g => g.Count() > 1)) throw new ArgumentException("Duplicate ingredient IDs are not allowed.");
    }
}
public sealed class RecipeIngredient
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public string FoodGroup { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal CaloriesPerUnit { get; set; }
}
