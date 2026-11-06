using System.Net;
using System.Net.Http.Json;

namespace DailyMealPlan.Api.Clients;

public sealed record RecipeLookup(int Id, string? Name, [property: System.Text.Json.Serialization.JsonRequired] decimal TotalCalories);

public sealed record IngredientLookup(int Id, string? Name, [property: System.Text.Json.Serialization.JsonRequired] decimal Calories);

public sealed class LookupNotFoundException(string referenceType, int id)
    : Exception($"The {referenceType} with ID {id} was not found.")
{
    public string ReferenceType { get; } = referenceType;
    public int Id { get; } = id;
}

public sealed class LookupUnavailableException(string referenceType, int id, Exception? innerException = null)
    : Exception($"The {referenceType} lookup is unavailable for ID {id}.", innerException)
{
    public string ReferenceType { get; } = referenceType;
    public int Id { get; } = id;
}

internal static class NutritionLookup
{
    public static async Task<T> GetAsync<T>(HttpClient client, string path, string type, int id, CancellationToken token)
    {
        try
        {
            using var response = await client.GetAsync(path, token);
            if (response.StatusCode == HttpStatusCode.NotFound)
                throw new LookupNotFoundException(type, id);
            if (!response.IsSuccessStatusCode)
                throw new LookupUnavailableException(type, id);
            return await response.Content.ReadFromJsonAsync<T>(token)
                ?? throw new LookupUnavailableException(type, id);
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException or NotSupportedException
            || ex is OperationCanceledException && !token.IsCancellationRequested)
        {
            throw new LookupUnavailableException(type, id, ex);
        }
    }
}

public sealed class RecipeLookupClient(HttpClient client)
{
    public Task<RecipeLookup> GetAsync(int id, CancellationToken token) =>
        NutritionLookup.GetAsync<RecipeLookup>(client, $"api/recipes/{id}", "recipe", id, token);
}

public sealed class IngredientLookupClient(HttpClient client)
{
    public Task<IngredientLookup> GetAsync(int id, CancellationToken token) =>
        NutritionLookup.GetAsync<IngredientLookup>(client, $"api/ingredients/{id}", "ingredient", id, token);
}
