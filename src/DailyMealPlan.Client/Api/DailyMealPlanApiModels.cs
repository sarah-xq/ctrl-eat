namespace DailyMealPlan.Client.Api;

public sealed record DailyMealPlanRequest
{
    public DateOnly Date { get; init; }
    public int? BreakfastRecipeId { get; init; }
    public int? LunchRecipeId { get; init; }
    public int? DinnerRecipeId { get; init; }
    public List<PlanSnackRequest> Snacks { get; init; } = [];
}

public sealed record PlanSnackRequest
{
    public bool IsRecipe { get; init; }
    public int ItemId { get; init; }
    public decimal Quantity { get; init; }
    public string? Unit { get; init; }
}

public sealed record DailyMealPlanResponse
{
    public int Id { get; init; }
    public DateOnly Date { get; init; }
    public int? BreakfastRecipeId { get; init; }
    public int? LunchRecipeId { get; init; }
    public int? DinnerRecipeId { get; init; }
    public List<PlanSnackResponse> Snacks { get; init; } = [];
    public decimal TotalCalories { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed record PlanSnackResponse
{
    public int Id { get; init; }
    public bool IsRecipe { get; init; }
    public int ItemId { get; init; }
    public string? Name { get; init; }
    public decimal Quantity { get; init; }
    public string? Unit { get; init; }
    public decimal Calories { get; init; }
}