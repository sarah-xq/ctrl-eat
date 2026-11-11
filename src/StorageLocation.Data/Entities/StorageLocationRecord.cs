namespace StorageLocation.Data.Entities;

public sealed class StorageLocationRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal MinimumTemperature { get; set; }
    public decimal MaximumTemperature { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<StorageLocationIngredientRecord> IngredientReferences { get; set; } = new List<StorageLocationIngredientRecord>();
}
