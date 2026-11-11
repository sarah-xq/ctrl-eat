namespace StorageLocation.Api.DTOs;

public sealed record StoredIngredientResponse(int Id, string Name, string FoodGroup, decimal Calories);
