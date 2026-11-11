namespace StorageLocation.Api.Clients;

public sealed class IngredientLookupUnavailableException : Exception
{
    public IngredientLookupUnavailableException(Exception? innerException = null)
        : base("Ingredient lookup is unavailable; ingredient references cannot be validated.", innerException)
    {
    }
}
