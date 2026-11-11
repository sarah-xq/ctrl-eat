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
    public decimal ProteinGrams { get; set; }
    public decimal CarbohydratesGrams { get; set; }
    public decimal FatGrams { get; set; }
    public decimal FiberGrams { get; set; }
    public decimal SugarGrams { get; set; }
    public decimal SodiumMilligrams { get; set; }
    public decimal CholesterolMilligrams { get; set; }
    public string VitaminsMineralsJson { get; set; } = "{}";
    public string AllergensJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<RecipeIngredient> Ingredients { get; set; } = new();

    public static void Validate(string? name, int totalTime, int cookTime, IEnumerable<(int id, decimal quantity)> ingredients)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200) throw new ArgumentException("Recipe name must be 1-200 characters.");
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

public sealed record NutritionAmounts(decimal ProteinGrams = 0, decimal CarbohydratesGrams = 0,
    decimal FatGrams = 0, decimal FiberGrams = 0, decimal SugarGrams = 0,
    decimal SodiumMilligrams = 0, decimal CholesterolMilligrams = 0)
{
    public NutritionAmounts PlusScaled(NutritionAmounts other, decimal quantity) => new(
        ProteinGrams + other.ProteinGrams * quantity,
        CarbohydratesGrams + other.CarbohydratesGrams * quantity,
        FatGrams + other.FatGrams * quantity,
        FiberGrams + other.FiberGrams * quantity,
        SugarGrams + other.SugarGrams * quantity,
        SodiumMilligrams + other.SodiumMilligrams * quantity,
        CholesterolMilligrams + other.CholesterolMilligrams * quantity);
}

public interface IRecipeRepository
{
    Task<IReadOnlyList<Recipe>> SearchAsync(string? name, string? author, CancellationToken cancellationToken);
    Task<Recipe?> GetAsync(int id, CancellationToken cancellationToken);
    Task AddAsync(Recipe recipe, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken);
    void ReplaceIngredients(Recipe recipe, List<RecipeIngredient> ingredients);
}
