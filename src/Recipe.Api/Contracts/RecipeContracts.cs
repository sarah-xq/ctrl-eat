using Recipe.Domain;

namespace Recipe.Api.Contracts;

public sealed record RecipeIngredientInput(int IngredientId, decimal Quantity, string Unit);
public sealed record RecipeInput(string Name, string? Author, int TotalTimeMinutes,
    int CookTimeMinutes, string? Description, List<RecipeIngredientInput>? Ingredients);
public enum AllergenPresence { Contains, MayContain, Trace }
public sealed record AllergenLabel(string Name, AllergenPresence Presence);
public sealed record NutritionLookup(decimal ProteinGrams = 0, decimal CarbohydratesGrams = 0,
    decimal FatGrams = 0, decimal FiberGrams = 0, decimal SugarGrams = 0,
    decimal SodiumMilligrams = 0, decimal CholesterolMilligrams = 0,
    Dictionary<string, decimal>? VitaminsMinerals = null);
public sealed record IngredientLookup(int Id, string Name, string FoodGroup, decimal Calories,
    NutritionLookup? Nutrition = null, List<AllergenLabel>? Allergens = null);
public sealed record RecipeIngredientResponse(int IngredientId, string IngredientName,
    string FoodGroup, decimal Quantity, string Unit, decimal CaloriesPerUnit);
public sealed record RecipeResponse(int Id, string Name, string? Author, int TotalTimeMinutes,
    int CookTimeMinutes, string Description, decimal TotalCalories, DateTime CreatedAt,
    IReadOnlyList<RecipeIngredientResponse> Ingredients, NutritionAmounts Nutrition,
    Dictionary<string, decimal> VitaminsMinerals, IReadOnlyList<AllergenLabel> Allergens);
