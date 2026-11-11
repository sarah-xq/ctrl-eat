namespace StorageLocation.Data.Entities;

public sealed class StorageLocationIngredientRecord
{
    public int StorageLocationId { get; set; }
    public int IngredientId { get; set; }
    public int Position { get; set; }
}
