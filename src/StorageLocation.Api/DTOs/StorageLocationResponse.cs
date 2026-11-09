namespace StorageLocation.Api.DTOs;

public sealed class StorageLocationResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public decimal MinimumTemperature { get; init; }
    public decimal MaximumTemperature { get; init; }
    public List<StoredIngredientResponse> Ingredients { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}
