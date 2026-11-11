namespace DailyMealPlan.Domain.Models;

public sealed class MealPlan
{
    public int Id { get; set; }
    public string Issuer { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public int? BreakfastRecipeId { get; set; }
    public int? LunchRecipeId { get; set; }
    public int? DinnerRecipeId { get; set; }
    public List<PlanSnack> Snacks { get; set; } = new();
    public decimal TotalCalories { get; private set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public MealPlan()
    {
        var now = DateTime.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public MealPlan(string issuer, string subject, DateOnly date)
        : this()
    {
        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new ArgumentException("Issuer is required.", nameof(issuer));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Subject is required.", nameof(subject));
        }

        Issuer = issuer;
        Subject = subject;
        Date = date;
    }

    public decimal CalculateTotalCalories(
        IReadOnlyDictionary<int, decimal> recipeCalories,
        IReadOnlyDictionary<int, decimal> ingredientCalories)
    {
        ArgumentNullException.ThrowIfNull(recipeCalories);
        ArgumentNullException.ThrowIfNull(ingredientCalories);

        decimal total = 0;
        total += GetMealCalories(BreakfastRecipeId, recipeCalories);
        total += GetMealCalories(LunchRecipeId, recipeCalories);
        total += GetMealCalories(DinnerRecipeId, recipeCalories);

        foreach (var snack in Snacks)
        {
            var calories = snack.IsRecipe
                ? recipeCalories[snack.ItemId]
                : ingredientCalories[snack.ItemId];

            total += snack.CalculateCalories(calories);
        }

        return total;
    }

    public void RecalculateTotalCalories(
        IReadOnlyDictionary<int, decimal> recipeCalories,
        IReadOnlyDictionary<int, decimal> ingredientCalories)
    {
        TotalCalories = CalculateTotalCalories(recipeCalories, ingredientCalories);
        UpdatedAt = DateTime.UtcNow;
    }

    private static decimal GetMealCalories(
        int? recipeId,
        IReadOnlyDictionary<int, decimal> recipeCalories)
    {
        return recipeId.HasValue ? recipeCalories[recipeId.Value] : 0m;
    }
}