using System.Text.Json;
using Ingredient.Api.Contracts;
using Ingredient.Domain;

namespace Ingredient.Api.Services;

public sealed class IngredientApplicationService(IIngredientRepository repository, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<IngredientResponse>> SearchAsync(string? name, string? group, string? allergen, CancellationToken ct) =>
        (await repository.SearchAsync(name, group, allergen, ct)).Select(ToResponse).ToList();

    public async Task<IngredientResponse?> GetAsync(int id, CancellationToken ct)
    {
        var row = await repository.GetAsync(id, ct);
        return row is null ? null : ToResponse(row);
    }

    public async Task<IngredientResponse> CreateAsync(IngredientInput input, CancellationToken ct)
    {
        Validate(input);
        var row = new Ingredient.Domain.Ingredient();
        await ApplyAsync(row, input, ct);
        await repository.AddAsync(row, ct);
        await repository.SaveChangesAsync(ct);
        return ToResponse(row);
    }

    public async Task<IngredientResponse?> UpdateAsync(int id, IngredientInput input, CancellationToken ct)
    {
        var row = await repository.GetAsync(id, ct);
        if (row is null) return null;
        Validate(input);
        await ApplyAsync(row, input, ct);
        await repository.SaveChangesAsync(ct);
        return ToResponse(row);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var row = await repository.GetAsync(id, ct);
        if (row is null) return false;
        await repository.DeleteAsync(row, ct);
        await repository.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<ExpiryAlertResponse>> AlertsAsync(CancellationToken ct)
    {
        var today = timeProvider.GetUtcNow().UtcDateTime;
        return (await repository.SearchAsync(null, null, null, ct))
            .Where(x => x.ExpiryDate.HasValue)
            .Select(x => new { Row = x, Status = IngredientRules.GetExpiryStatus(x.ExpiryDate,
                x.AlertThresholdDays, today) })
            .Where(x => x.Status is ExpiryStatus.SoonExpiring or ExpiryStatus.Expired)
            .Select(x => new ExpiryAlertResponse(x.Row.Id, x.Row.Name, x.Row.ExpiryDate!.Value,
                (x.Row.ExpiryDate.Value.Date - today.Date).Days, x.Status))
            .OrderBy(x => x.DaysUntilExpiry).ToList();
    }

    private static void Validate(IngredientInput input)
    {
        Ingredient.Domain.Ingredient.Validate(input.Name, input.FoodGroup, input.Calories);
        IngredientRules.ValidateStorage(input.StorageMethod, input.BatchNumber, input.AlertThresholdDays);
        var allergens = input.Allergens ?? new();
        if (allergens.Any(a => string.IsNullOrWhiteSpace(a.Name) || a.Name.Length > 100
            || !Enum.IsDefined(a.Presence)))
            throw new ArgumentException("Allergen names must be 1-100 characters and presence must be valid.");
        if (allergens.Select(x => x.Name.Trim().ToLowerInvariant()).Distinct().Count() != allergens.Count)
            throw new ArgumentException("Duplicate allergens are not allowed.");
        if (input.Nutrition is { } n)
        {
            var data = ToNutrition(n);
            data.Validate();
            if ((n.VitaminsMinerals ?? new()).Any(pair => string.IsNullOrWhiteSpace(pair.Key)
                || pair.Key.Length > 100 || pair.Value < 0))
                throw new ArgumentException("Vitamins and minerals must have names and nonnegative amounts.");
        }
    }

    private async Task ApplyAsync(Ingredient.Domain.Ingredient row, IngredientInput input, CancellationToken ct)
    {
        row.Name = input.Name.Trim();
        row.FoodGroup = input.FoodGroup.Trim();
        row.Calories = input.Calories;
        row.ExpiryDate = input.ExpiryDate;
        row.StorageMethod = input.StorageMethod;
        row.BatchNumber = input.BatchNumber?.Trim();
        row.AlertThresholdDays = input.AlertThresholdDays;
        if (input.Nutrition is null) row.Nutrition = null;
        else
        {
            var n = ToNutrition(input.Nutrition);
            if (row.Nutrition is null) row.Nutrition = n;
            else
            {
                row.Nutrition.ProteinGrams = n.ProteinGrams;
                row.Nutrition.CarbohydratesGrams = n.CarbohydratesGrams;
                row.Nutrition.FatGrams = n.FatGrams;
                row.Nutrition.FiberGrams = n.FiberGrams;
                row.Nutrition.SugarGrams = n.SugarGrams;
                row.Nutrition.SodiumMilligrams = n.SodiumMilligrams;
                row.Nutrition.CholesterolMilligrams = n.CholesterolMilligrams;
                row.Nutrition.VitaminsMineralsJson = n.VitaminsMineralsJson;
                row.Nutrition.ServingSize = n.ServingSize;
                row.Nutrition.DataSource = n.DataSource;
                row.Nutrition.SourceDate = n.SourceDate;
            }
        }
        var desired = new List<(Allergen Allergen, AllergenPresence Presence)>();
        foreach (var inputAllergen in input.Allergens ?? new())
            desired.Add((await repository.GetOrCreateAllergenAsync(inputAllergen.Name, ct), inputAllergen.Presence));
        foreach (var existing in row.Allergens.ToList())
        {
            var kept = desired.FirstOrDefault(x => x.Allergen.Name == existing.Allergen.Name);
            if (kept.Allergen is null) row.Allergens.Remove(existing);
            else existing.Presence = kept.Presence;
        }
        foreach (var (allergen, presence) in desired)
            if (!row.Allergens.Any(x => x.Allergen.Name == allergen.Name))
                row.Allergens.Add(new IngredientAllergen { Allergen = allergen, Presence = presence });
    }

    private static NutritionalInfo ToNutrition(NutritionInput n) => new()
    {
        ProteinGrams = n.ProteinGrams, CarbohydratesGrams = n.CarbohydratesGrams,
        FatGrams = n.FatGrams, FiberGrams = n.FiberGrams, SugarGrams = n.SugarGrams,
        SodiumMilligrams = n.SodiumMilligrams, CholesterolMilligrams = n.CholesterolMilligrams,
        VitaminsMineralsJson = JsonSerializer.Serialize(n.VitaminsMinerals ?? new()),
        ServingSize = n.ServingSize, DataSource = n.DataSource, SourceDate = n.SourceDate
    };

    private IngredientResponse ToResponse(Ingredient.Domain.Ingredient x)
    {
        var today = timeProvider.GetUtcNow().UtcDateTime;
        var status = IngredientRules.GetExpiryStatus(x.ExpiryDate, x.AlertThresholdDays, today);
        NutritionInput? nutrition = null;
        if (x.Nutrition is { } n)
            nutrition = new NutritionInput(n.ProteinGrams, n.CarbohydratesGrams, n.FatGrams,
                n.FiberGrams, n.SugarGrams, n.SodiumMilligrams, n.CholesterolMilligrams,
                JsonSerializer.Deserialize<Dictionary<string, decimal>>(n.VitaminsMineralsJson) ?? new(),
                n.ServingSize, n.DataSource, n.SourceDate);
        return new IngredientResponse(x.Id, x.Name, x.FoodGroup, x.ExpiryDate, x.Calories,
            x.CreatedAt, x.StorageMethod, x.BatchNumber, x.AlertThresholdDays, status,
            x.ExpiryDate is null ? null : (x.ExpiryDate.Value.Date - today.Date).Days,
            nutrition, x.Allergens.Select(a => new AllergenInput(a.Allergen.Name, a.Presence)).ToList());
    }
}
