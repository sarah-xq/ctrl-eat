namespace Ingredient.Domain;

public sealed class Ingredient
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FoodGroup { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public decimal Calories { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static void Validate(string? name, string? foodGroup, decimal calories)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Ingredient name is required.");
        if (string.IsNullOrWhiteSpace(foodGroup)) throw new ArgumentException("Food group is required.");
        if (calories < 0) throw new ArgumentException("Calories cannot be negative.");
    }
}
