namespace StorageLocation.Api.Clients;

public interface IIngredientClient
{
    Task<IngredientLookupResult?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
