using System.ComponentModel.DataAnnotations;

namespace DailyMealPlan.Api.DTOs;

public sealed record DailyMealPlanRequest(
    DateOnly Date,
    int? BreakfastRecipeId,
    int? LunchRecipeId,
    int? DinnerRecipeId,
    List<PlanSnackRequest>? Snacks);

public sealed record PlanSnackRequest(
    bool IsRecipe,
    int ItemId,
    decimal Quantity,
    string? Unit);

public sealed record DailyMealPlanResponse(
    int Id,
    DateOnly Date,
    int? BreakfastRecipeId,
    int? LunchRecipeId,
    int? DinnerRecipeId,
    IReadOnlyList<PlanSnackResponse> Snacks,
    decimal TotalCalories,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record PlanSnackResponse(
    int Id,
    bool IsRecipe,
    int ItemId,
    string? Name,
    decimal Quantity,
    string? Unit,
    decimal Calories);