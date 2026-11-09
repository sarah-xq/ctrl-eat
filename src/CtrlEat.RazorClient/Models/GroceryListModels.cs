using System.ComponentModel.DataAnnotations;

namespace CtrlEat.RazorClient.Models;

public sealed class GroceryListRequest
{
    [Required]
    public DateTime Date { get; set; }
    public List<int>? IngredientIds { get; set; } = [];
}

public sealed class GroceryListResponse
{
    public int Id { get; init; }
    public DateTime Date { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<GroceryListIngredient> Ingredients { get; init; } = [];
}

public sealed record GroceryListIngredient(int Id, string Name, string FoodGroup, decimal Calories);
