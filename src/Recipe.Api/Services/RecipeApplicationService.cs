using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Recipe.Api.Contracts;
using Recipe.Domain;

namespace Recipe.Api.Services;

public sealed record RecipeOutcome(RecipeResponse? Value, int Status, string? Error = null);

public sealed class RecipeApplicationService(IRecipeRepository repository, IHttpClientFactory clients)
{
    public async Task<IReadOnlyList<RecipeResponse>> SearchAsync(string? name, string? author, CancellationToken ct) =>
        (await repository.SearchAsync(name, author, ct)).Select(ToResponse).ToList();

    public async Task<RecipeResponse?> GetAsync(int id, CancellationToken ct)
    {
        var row = await repository.GetAsync(id, ct);
        return row is null ? null : ToResponse(row);
    }

    public async Task<RecipeOutcome> CreateAsync(RecipeInput input, string bearer, CancellationToken ct)
    {
        var assembled = await ComposeAsync(input, bearer, ct);
        if (assembled.Error is not null) return assembled.Error;
        await repository.AddAsync(assembled.Recipe!, ct);
        await repository.SaveChangesAsync(ct);
        return new RecipeOutcome(ToResponse(assembled.Recipe!), 201);
    }

    public async Task<RecipeOutcome> UpdateAsync(int id, RecipeInput input, string bearer, CancellationToken ct)
    {
        var current = await repository.GetAsync(id, ct);
        if (current is null) return new RecipeOutcome(null, 404);
        var assembled = await ComposeAsync(input, bearer, ct);
        if (assembled.Error is not null) return assembled.Error;
        var updated = assembled.Recipe!;
        current.Name = updated.Name;
        current.Author = updated.Author;
        current.Description = updated.Description;
        current.TotalTimeMinutes = updated.TotalTimeMinutes;
        current.CookTimeMinutes = updated.CookTimeMinutes;
        current.TotalCalories = updated.TotalCalories;
        current.ProteinGrams = updated.ProteinGrams;
        current.CarbohydratesGrams = updated.CarbohydratesGrams;
        current.FatGrams = updated.FatGrams;
        current.FiberGrams = updated.FiberGrams;
        current.SugarGrams = updated.SugarGrams;
        current.SodiumMilligrams = updated.SodiumMilligrams;
        current.CholesterolMilligrams = updated.CholesterolMilligrams;
        current.VitaminsMineralsJson = updated.VitaminsMineralsJson;
        current.AllergensJson = updated.AllergensJson;
        repository.ReplaceIngredients(current, updated.Ingredients);
        await repository.SaveChangesAsync(ct);
        return new RecipeOutcome(ToResponse(current), 200);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var row = await repository.GetAsync(id, ct);
        if (row is null) return false;
        await repository.DeleteAsync(row, ct);
        await repository.SaveChangesAsync(ct);
        return true;
    }

    private async Task<(Recipe.Domain.Recipe? Recipe, RecipeOutcome? Error)> ComposeAsync(
        RecipeInput input, string bearer, CancellationToken ct)
    {
        try
        {
            Recipe.Domain.Recipe.Validate(input.Name, input.TotalTimeMinutes, input.CookTimeMinutes,
                (input.Ingredients ?? new()).Select(x => (x.IngredientId, x.Quantity)));
        }
        catch (ArgumentException ex)
        {
            return (null, new RecipeOutcome(null, 400, ex.Message));
        }
        if (!AuthenticationHeaderValue.TryParse(bearer, out var auth)
            || !string.Equals(auth.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(auth.Parameter))
            return (null, new RecipeOutcome(null, 401));

        var row = new Recipe.Domain.Recipe
        {
            Name = input.Name.Trim(), Author = input.Author, Description = input.Description ?? "",
            TotalTimeMinutes = input.TotalTimeMinutes, CookTimeMinutes = input.CookTimeMinutes
        };
        var totals = new NutritionAmounts();
        var vitamins = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var allergens = new Dictionary<string, AllergenLabel>(StringComparer.OrdinalIgnoreCase);
        var client = clients.CreateClient("IngredientApi");
        foreach (var item in input.Ingredients ?? new())
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/Ingredients/{item.IngredientId}");
            request.Headers.Authorization = auth;
            HttpResponseMessage response;
            try { response = await client.SendAsync(request, ct); }
            catch (HttpRequestException) { return (null, new RecipeOutcome(null, 503, "Ingredient service unavailable.")); }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            { return (null, new RecipeOutcome(null, 503, "Ingredient lookup timed out.")); }
            using (response)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return (null, new RecipeOutcome(null, 400, $"Ingredient {item.IngredientId} not found."));
                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                    return (null, new RecipeOutcome(null, 502, "Ingredient lookup token was not authorized."));
                if (!response.IsSuccessStatusCode)
                    return (null, new RecipeOutcome(null, 503, "Ingredient lookup failed."));
                IngredientLookup? ingredient;
                try { ingredient = await response.Content.ReadFromJsonAsync<IngredientLookup>(cancellationToken: ct); }
                catch (JsonException)
                { return (null, new RecipeOutcome(null, 502, "Invalid Ingredient response.")); }
                if (ingredient is null || ingredient.Id != item.IngredientId)
                    return (null, new RecipeOutcome(null, 502, "Invalid Ingredient response."));
                row.Ingredients.Add(new RecipeIngredient
                {
                    IngredientId = item.IngredientId, IngredientName = ingredient.Name,
                    FoodGroup = ingredient.FoodGroup, CaloriesPerUnit = ingredient.Calories,
                    Quantity = item.Quantity, Unit = item.Unit
                });
                row.TotalCalories += ingredient.Calories * item.Quantity;
                if (ingredient.Nutrition is { } n)
                {
                    totals = totals.PlusScaled(new NutritionAmounts(n.ProteinGrams, n.CarbohydratesGrams,
                        n.FatGrams, n.FiberGrams, n.SugarGrams, n.SodiumMilligrams,
                        n.CholesterolMilligrams), item.Quantity);
                    foreach (var (name, amount) in n.VitaminsMinerals ?? new())
                        vitamins[name] = vitamins.GetValueOrDefault(name) + amount * item.Quantity;
                }
                foreach (var a in ingredient.Allergens ?? new())
                {
                    if (!allergens.TryGetValue(a.Name, out var prior) || (int)a.Presence < (int)prior.Presence)
                        allergens[a.Name] = a;
                }
            }
        }
        row.ProteinGrams = totals.ProteinGrams;
        row.CarbohydratesGrams = totals.CarbohydratesGrams;
        row.FatGrams = totals.FatGrams;
        row.FiberGrams = totals.FiberGrams;
        row.SugarGrams = totals.SugarGrams;
        row.SodiumMilligrams = totals.SodiumMilligrams;
        row.CholesterolMilligrams = totals.CholesterolMilligrams;
        row.VitaminsMineralsJson = JsonSerializer.Serialize(vitamins);
        row.AllergensJson = JsonSerializer.Serialize(allergens.Values.OrderBy(x => x.Name).ToList());
        return (row, null);
    }

    private static RecipeResponse ToResponse(Recipe.Domain.Recipe x) => new(
        x.Id, x.Name, x.Author, x.TotalTimeMinutes, x.CookTimeMinutes, x.Description,
        x.TotalCalories, x.CreatedAt,
        x.Ingredients.Select(i => new RecipeIngredientResponse(i.IngredientId, i.IngredientName,
            i.FoodGroup, i.Quantity, i.Unit, i.CaloriesPerUnit)).ToList(),
        new NutritionAmounts(x.ProteinGrams, x.CarbohydratesGrams, x.FatGrams,
            x.FiberGrams, x.SugarGrams, x.SodiumMilligrams, x.CholesterolMilligrams),
        JsonSerializer.Deserialize<Dictionary<string, decimal>>(x.VitaminsMineralsJson) ?? new(),
        JsonSerializer.Deserialize<List<AllergenLabel>>(x.AllergensJson) ?? new());
}
