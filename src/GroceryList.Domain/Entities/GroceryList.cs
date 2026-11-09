namespace GroceryList.Domain.Entities;

public sealed class GroceryList
{
    private readonly List<int> _ingredientIds = [];

    public int Id { get; set; }
    public IReadOnlyList<int> IngredientIds => _ingredientIds.AsReadOnly();
    public DateTime Date { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public GroceryList(DateTime date, IEnumerable<int>? ingredientIds = null)
    {
        Date = ValidateAndNormalizeDate(date);
        _ingredientIds.AddRange(NormalizeIngredientIds(ingredientIds));
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(DateTime date, IEnumerable<int>? ingredientIds = null)
    {
        var normalizedDate = ValidateAndNormalizeDate(date);
        var normalizedIngredientIds = NormalizeIngredientIds(ingredientIds);

        Date = normalizedDate;
        _ingredientIds.Clear();
        _ingredientIds.AddRange(normalizedIngredientIds);
        UpdatedAt = DateTime.UtcNow;
    }

    public static GroceryList Rehydrate(
        int id,
        DateTime date,
        IEnumerable<int>? ingredientIds,
        DateTime createdAt,
        DateTime updatedAt)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), id, "Persisted grocery list IDs must be greater than zero.");
        }

        var groceryList = new GroceryList(date, ingredientIds)
        {
            Id = id,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        return groceryList;
    }

    private static DateTime ValidateAndNormalizeDate(DateTime date)
    {
        if (date == default)
        {
            throw new ArgumentException("Grocery list date is required.", nameof(date));
        }

        return date.Date;
    }

    private static List<int> NormalizeIngredientIds(IEnumerable<int>? ingredientIds)
    {
        var normalizedIds = new List<int>();
        var seenIds = new HashSet<int>();

        foreach (var ingredientId in ingredientIds ?? [])
        {
            if (ingredientId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ingredientIds),
                    ingredientId,
                    "Ingredient IDs must be greater than zero.");
            }

            if (seenIds.Add(ingredientId))
            {
                normalizedIds.Add(ingredientId);
            }
        }

        return normalizedIds;
    }
}