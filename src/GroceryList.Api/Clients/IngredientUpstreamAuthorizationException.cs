namespace GroceryList.Api.Clients;

public sealed class IngredientUpstreamAuthorizationException(int statusCode)
    : Exception($"Ingredient service rejected the request with HTTP {statusCode}.")
{
    public int StatusCode { get; } = statusCode;
}
