namespace Ingredient.Domain;

public sealed class Ingredient
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FoodGroup { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public decimal Calories { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string StorageMethod { get; set; } = "RoomTemperature";
    public string? BatchNumber { get; set; }
    public int AlertThresholdDays { get; set; } = 7;
    public NutritionalInfo? Nutrition { get; set; }
    public List<IngredientAllergen> Allergens { get; set; } = new();

    public static void Validate(string? name, string? foodGroup, decimal calories)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
            throw new ArgumentException("Ingredient name must be 1-200 characters.");
        if (string.IsNullOrWhiteSpace(foodGroup) || foodGroup.Length > 100)
            throw new ArgumentException("Food group must be 1-100 characters.");
        if (calories < 0) throw new ArgumentException("Calories cannot be negative.");
    }
}

public sealed class NutritionalInfo
{
    public int IngredientId { get; set; }
    public decimal ProteinGrams { get; set; }
    public decimal CarbohydratesGrams { get; set; }
    public decimal FatGrams { get; set; }
    public decimal FiberGrams { get; set; }
    public decimal SugarGrams { get; set; }
    public decimal SodiumMilligrams { get; set; }
    public decimal CholesterolMilligrams { get; set; }
    public string VitaminsMineralsJson { get; set; } = "{}";
    public string ServingSize { get; set; } = "1 serving";
    public string? DataSource { get; set; }
    public DateTime? SourceDate { get; set; }

    public void Validate()
    {
        if (new[] { ProteinGrams, CarbohydratesGrams, FatGrams, FiberGrams, SugarGrams,
            SodiumMilligrams, CholesterolMilligrams }.Any(v => v < 0))
            throw new ArgumentException("Nutritional amounts cannot be negative.");
        if (string.IsNullOrWhiteSpace(ServingSize) || ServingSize.Length > 100)
            throw new ArgumentException("Serving size must be 1-100 characters.");
        if (DataSource?.Length > 200)
            throw new ArgumentException("Nutritional data source must not exceed 200 characters.");
    }
}

public enum AllergenPresence { Contains, MayContain, Trace }
public sealed class Allergen
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
public sealed class IngredientAllergen
{
    public int IngredientId { get; set; }
    public int AllergenId { get; set; }
    public Allergen Allergen { get; set; } = null!;
    public AllergenPresence Presence { get; set; }
}

public enum ExpiryStatus { NoDate, Fresh, SoonExpiring, Expired }
public sealed record ExpiryAlert(int IngredientId, string Name, DateTime ExpiryDate,
    int DaysUntilExpiry, ExpiryStatus Status);

public static class IngredientRules
{
    public static ExpiryStatus GetExpiryStatus(DateTime? expiryDate, int warningDays, DateTime utcNow)
    {
        if (warningDays < 0 || warningDays > 365)
            throw new ArgumentException("Alert threshold must be between 0 and 365 days.");
        if (expiryDate is null) return ExpiryStatus.NoDate;
        var remaining = (expiryDate.Value.Date - utcNow.Date).Days;
        return remaining < 0 ? ExpiryStatus.Expired
            : remaining <= warningDays ? ExpiryStatus.SoonExpiring : ExpiryStatus.Fresh;
    }

    public static void ValidateStorage(string storageMethod, string? batchNumber, int alertThresholdDays)
    {
        if (storageMethod is not ("RoomTemperature" or "Refrigerated" or "Frozen"))
            throw new ArgumentException("StorageMethod must be RoomTemperature, Refrigerated or Frozen.");
        if (batchNumber?.Length > 100)
            throw new ArgumentException("Batch number must not exceed 100 characters.");
        if (alertThresholdDays < 0 || alertThresholdDays > 365)
            throw new ArgumentException("Alert threshold must be between 0 and 365 days.");
    }
}

public interface IIngredientRepository
{
    Task<IReadOnlyList<Ingredient>> SearchAsync(string? name, string? group, string? allergen, CancellationToken cancellationToken);
    Task<Ingredient?> GetAsync(int id, CancellationToken cancellationToken);
    Task AddAsync(Ingredient ingredient, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task DeleteAsync(Ingredient ingredient, CancellationToken cancellationToken);
    Task<Allergen> GetOrCreateAllergenAsync(string name, CancellationToken cancellationToken);
}
