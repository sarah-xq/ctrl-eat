using System.Globalization;
using CtrlEat.RazorClient.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CtrlEat.RazorClient.Pages.GroceryLists;

public abstract class FormModel : PageModel
{
    [BindProperty] public GroceryListRequest Input { get; set; } = new() { Date = DateTime.Today };
    [BindProperty] public string IngredientIdsText { get; set; } = string.Empty;
    public string? ErrorMessage { get; protected set; }
    public bool Loaded { get; protected set; } = true;
    public string SubmitLabel { get; protected set; } = "Save";

    protected bool PrepareInput()
    {
        if (Input.Date == default)
        {
            ModelState.AddModelError("Input.Date", "Date is required.");
        }

        var ids = new List<int>();
        foreach (var value in IngredientIdsText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) || id <= 0)
            {
                ModelState.AddModelError(nameof(IngredientIdsText), "Ingredient IDs must be positive whole numbers.");
            }
            else if (!ids.Contains(id))
            {
                ids.Add(id);
            }
        }

        Input.Date = Input.Date.Date;
        Input.IngredientIds = ids;
        return ModelState.IsValid;
    }

    protected void SetInput(GroceryListResponse groceryList)
    {
        Input = new GroceryListRequest
        {
            Date = groceryList.Date.Date,
            IngredientIds = groceryList.Ingredients.Select(ingredient => ingredient.Id).ToList()
        };
        IngredientIdsText = string.Join(", ", Input.IngredientIds);
    }
}
