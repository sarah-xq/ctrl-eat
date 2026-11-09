namespace GroceryList.Api.Clients;

public sealed class UnavailableIngredientClient : IIngredientClient
{
    public Task<IngredientLookupResult?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromException<IngredientLookupResult?>(new IngredientLookupUnavailableException());
    }
}
