using Ingredient.Domain;

namespace Ingredient.Api.Contracts;

public sealed record NutritionInput(
    decimal ProteinGrams = 0, decimal CarbohydratesGrams = 0, decimal FatGrams = 0,
    decimal FiberGrams = 0, decimal SugarGrams = 0, decimal SodiumMilligrams = 0,
    decimal CholesterolMilligrams = 0, Dictionary<string, decimal>? VitaminsMinerals = null,
    string ServingSize = "1 serving", string? DataSource = null, DateTime? SourceDate = null);

public sealed record AllergenInput(string Name, AllergenPresence Presence = AllergenPresence.Contains);

public sealed record IngredientInput(string Name, string FoodGroup, DateTime? ExpiryDate,
    decimal Calories, NutritionInput? Nutrition = null, List<AllergenInput>? Allergens = null,
    string StorageMethod = "RoomTemperature", string? BatchNumber = null, int AlertThresholdDays = 7);

public sealed record IngredientResponse(int Id, string Name, string FoodGroup, DateTime? ExpiryDate,
    decimal Calories, DateTime CreatedAt, string StorageMethod, string? BatchNumber,
    int AlertThresholdDays, ExpiryStatus ExpiryStatus, int? DaysUntilExpiry,
    NutritionInput? Nutrition, IReadOnlyList<AllergenInput> Allergens);

public sealed record ExpiryAlertResponse(int IngredientId, string Name, DateTime ExpiryDate,
    int DaysUntilExpiry, ExpiryStatus Status);
