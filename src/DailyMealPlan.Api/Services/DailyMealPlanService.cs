using DailyMealPlan.Api.Clients;
using DailyMealPlan.Api.DTOs;
using DailyMealPlan.Data.Repositories;
using DailyMealPlan.Domain.Models;

namespace DailyMealPlan.Api.Services;

public sealed class DailyMealPlanValidationException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}

public sealed class DailyMealPlanLookupUnavailableException(Exception innerException)
    : Exception("A recipe or ingredient lookup service is unavailable.", innerException);

public sealed class DailyMealPlanService(
    IDailyMealPlanRepository repository,
    RecipeLookupClient recipeClient,
    IngredientLookupClient ingredientClient)
{
    public async Task<DailyMealPlanResponse> CreateAsync(
        DailyMealPlanRequest request,
        string issuer,
        string subject,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var plan = new MealPlan(issuer, subject, request.Date);
        ApplyRequest(plan, request);
        await ResolveReferencesAndCalculateAsync(plan, cancellationToken);
        var created = await repository.CreateAsync(plan, cancellationToken);
        return Map(created);
    }

    public async Task<IReadOnlyList<DailyMealPlanResponse>> ListAsync(
        string issuer,
        string subject,
        DateOnly? date,
        CancellationToken cancellationToken = default)
    {
        var plans = await repository.ListAsync(issuer, subject, date, cancellationToken);
        return plans.Select(Map).ToList();
    }

    public async Task<DailyMealPlanResponse?> GetAsync(
        int id,
        string issuer,
        string subject,
        CancellationToken cancellationToken = default)
    {
        var plan = await repository.GetByIdAsync(id, issuer, subject, cancellationToken);
        return plan is null ? null : Map(plan);
    }

    public async Task<DailyMealPlanResponse?> UpdateAsync(
        int id,
        DailyMealPlanRequest request,
        string issuer,
        string subject,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var existing = await repository.GetByIdAsync(id, issuer, subject, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var replacement = new MealPlan(issuer, subject, request.Date)
        {
            Id = existing.Id,
            CreatedAt = existing.CreatedAt
        };
        ApplyRequest(replacement, request);
        await ResolveReferencesAndCalculateAsync(replacement, cancellationToken);

        if (!await repository.UpdateAsync(replacement, issuer, subject, cancellationToken))
        {
            return null;
        }

        return await GetAsync(id, issuer, subject, cancellationToken);
    }

    public Task<bool> DeleteAsync(
        int id,
        string issuer,
        string subject,
        CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(id, issuer, subject, cancellationToken);

    private static void ApplyRequest(MealPlan plan, DailyMealPlanRequest request)
    {
        if (request.Date == default)
            throw new DailyMealPlanValidationException("Date", "Enter a meal plan date.");
        plan.BreakfastRecipeId = request.BreakfastRecipeId;
        plan.LunchRecipeId = request.LunchRecipeId;
        plan.DinnerRecipeId = request.DinnerRecipeId;
        plan.Snacks = new List<PlanSnack>();

        foreach (var (snackRequest, index) in (request.Snacks ?? new List<PlanSnackRequest>()).Select((value, index) => (value, index)))
        {
            if (snackRequest is null)
                throw new DailyMealPlanValidationException($"Snacks[{index}]", "Enter a snack item.");
            try
            {
                plan.Snacks.Add(new PlanSnack(
                    snackRequest.IsRecipe,
                    snackRequest.ItemId,
                    snackRequest.Quantity,
                    snackRequest.Unit));
            }
            catch (ArgumentOutOfRangeException exception)
            {
                throw new DailyMealPlanValidationException(
                    $"Snacks[{index}].Quantity",
                    exception.Message);
            }
        }
    }

    private async Task ResolveReferencesAndCalculateAsync(
        MealPlan plan,
        CancellationToken cancellationToken)
    {
        var recipes = new Dictionary<int, RecipeLookup>();
        var ingredients = new Dictionary<int, IngredientLookup>();

        await ResolveRecipeAsync(plan.BreakfastRecipeId, nameof(plan.BreakfastRecipeId), recipes, cancellationToken);
        await ResolveRecipeAsync(plan.LunchRecipeId, nameof(plan.LunchRecipeId), recipes, cancellationToken);
        await ResolveRecipeAsync(plan.DinnerRecipeId, nameof(plan.DinnerRecipeId), recipes, cancellationToken);

        for (var index = 0; index < plan.Snacks.Count; index++)
        {
            var snack = plan.Snacks[index];
            var field = $"Snacks[{index}].ItemId";
            if (snack.IsRecipe)
            {
                await ResolveRecipeAsync(snack.ItemId, field, recipes, cancellationToken);
            }
            else
            {
                await ResolveIngredientAsync(snack.ItemId, field, ingredients, cancellationToken);
            }
        }

        var recipeCalories = recipes.ToDictionary(pair => pair.Key, pair => pair.Value.TotalCalories);
        var ingredientCalories = ingredients.ToDictionary(pair => pair.Key, pair => pair.Value.Calories);
        foreach (var snack in plan.Snacks)
        {
            string? name;
            decimal caloriesPerUnit;
            if (snack.IsRecipe)
            {
                var recipe = recipes[snack.ItemId];
                name = recipe.Name;
                caloriesPerUnit = recipe.TotalCalories;
            }
            else
            {
                var ingredient = ingredients[snack.ItemId];
                name = ingredient.Name;
                caloriesPerUnit = ingredient.Calories;
            }

            snack.Name = name;
            snack.Calories = snack.CalculateCalories(caloriesPerUnit);
        }

        plan.RecalculateTotalCalories(recipeCalories, ingredientCalories);
    }

    private async Task ResolveRecipeAsync(
        int? id,
        string field,
        IDictionary<int, RecipeLookup> recipes,
        CancellationToken cancellationToken)
    {
        if (!id.HasValue || recipes.ContainsKey(id.Value))
        {
            return;
        }

        try
        {
            recipes[id.Value] = await recipeClient.GetAsync(id.Value, cancellationToken);
        }
        catch (LookupNotFoundException exception)
        {
            throw new DailyMealPlanValidationException(
                field,
                $"Invalid {field}: recipe ID {exception.Id} was not found.");
        }
        catch (LookupUnavailableException exception)
        {
            throw new DailyMealPlanLookupUnavailableException(exception);
        }
    }

    private async Task ResolveIngredientAsync(
        int id,
        string field,
        IDictionary<int, IngredientLookup> ingredients,
        CancellationToken cancellationToken)
    {
        if (ingredients.ContainsKey(id))
        {
            return;
        }

        try
        {
            ingredients[id] = await ingredientClient.GetAsync(id, cancellationToken);
        }
        catch (LookupNotFoundException exception)
        {
            throw new DailyMealPlanValidationException(
                field,
                $"Invalid {field}: ingredient ID {exception.Id} was not found.");
        }
        catch (LookupUnavailableException exception)
        {
            throw new DailyMealPlanLookupUnavailableException(exception);
        }
    }

    private static DailyMealPlanResponse Map(MealPlan plan) =>
        new(
            plan.Id,
            plan.Date,
            plan.BreakfastRecipeId,
            plan.LunchRecipeId,
            plan.DinnerRecipeId,
            plan.Snacks.Select(snack => new PlanSnackResponse(
                snack.Id,
                snack.IsRecipe,
                snack.ItemId,
                snack.Name,
                snack.Quantity,
                snack.Unit,
                snack.Calories)).ToList(),
            plan.TotalCalories,
            plan.CreatedAt,
            plan.UpdatedAt);
}