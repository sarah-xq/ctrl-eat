namespace StorageLocation.Domain.Entities;

public class StorageLocation
{
    private readonly List<int> _ingredientIds = [];

    public int Id { get; set; }
    public string Name { get; private set; }
    public string Type { get; private set; }
    public decimal MinimumTemperature { get; private set; }
    public decimal MaximumTemperature { get; private set; }
    public IReadOnlyList<int> IngredientIds => _ingredientIds.AsReadOnly();
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public StorageLocation(
        string? name,
        string? type,
        decimal minimumTemperature,
        decimal maximumTemperature,
        IEnumerable<int>? ingredientIds = null)
    {
        var normalized = ValidateAndNormalize(name, type, minimumTemperature, maximumTemperature, ingredientIds);
        Name = normalized.Name;
        Type = normalized.Type;
        MinimumTemperature = minimumTemperature;
        MaximumTemperature = maximumTemperature;
        _ingredientIds.AddRange(normalized.IngredientIds);
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(
        string? name,
        string? type,
        decimal minimumTemperature,
        decimal maximumTemperature,
        IEnumerable<int>? ingredientIds = null)
    {
        var normalized = ValidateAndNormalize(name, type, minimumTemperature, maximumTemperature, ingredientIds);
        Name = normalized.Name;
        Type = normalized.Type;
        MinimumTemperature = minimumTemperature;
        MaximumTemperature = maximumTemperature;
        _ingredientIds.Clear();
        _ingredientIds.AddRange(normalized.IngredientIds);
        UpdatedAt = DateTime.UtcNow;
    }

    public static StorageLocation Rehydrate(
        int id,
        string? name,
        string? type,
        decimal minimumTemperature,
        decimal maximumTemperature,
        IEnumerable<int>? ingredientIds,
        DateTime createdAt,
        DateTime updatedAt)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), id, "Persisted storage location IDs must be greater than zero.");
        }

        var location = new StorageLocation(name, type, minimumTemperature, maximumTemperature, ingredientIds);
        if (!string.Equals(location.Name, name, StringComparison.Ordinal)
            || !string.Equals(location.Type, type, StringComparison.Ordinal))
        {
            throw new ArgumentException("Persisted storage location names and types must already be trimmed.");
        }

        location.Id = id;
        location.CreatedAt = createdAt;
        location.UpdatedAt = updatedAt;
        return location;
    }

    private static (string Name, string Type, List<int> IngredientIds) ValidateAndNormalize(
        string? name,
        string? type,
        decimal minimumTemperature,
        decimal maximumTemperature,
        IEnumerable<int>? ingredientIds)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Storage location name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Storage location type is required.", nameof(type));
        }

        if (minimumTemperature > maximumTemperature)
        {
            throw new ArgumentException(
                "Minimum temperature cannot be greater than maximum temperature.",
                nameof(minimumTemperature));
        }

        var normalizedIngredientIds = new List<int>();
        var seenIngredientIds = new HashSet<int>();
        foreach (var ingredientId in ingredientIds ?? [])
        {
            if (ingredientId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ingredientIds),
                    ingredientId,
                    "Ingredient IDs must be greater than zero.");
            }

            if (seenIngredientIds.Add(ingredientId))
            {
                normalizedIngredientIds.Add(ingredientId);
            }
        }

        return (name.Trim(), type.Trim(), normalizedIngredientIds);
    }
}
