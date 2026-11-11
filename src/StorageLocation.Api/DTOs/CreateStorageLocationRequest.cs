namespace StorageLocation.Api.DTOs;

public sealed class CreateStorageLocationRequest
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public decimal MinimumTemperature { get; set; }
    public decimal MaximumTemperature { get; set; }
    public List<int>? IngredientIds { get; set; } = [];
}
