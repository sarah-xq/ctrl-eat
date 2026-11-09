using System.ComponentModel.DataAnnotations;
using CtrlEat.RazorClient.Models;
using CtrlEat.RazorClient.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CtrlEat.RazorClient.Pages.StorageLocations;

public abstract class FormModel : PageModel
{
    [BindProperty] public StorageLocationRequest Input { get; set; } = new();
    [BindProperty] public string IngredientIdsText { get; set; } = string.Empty;
    public string? ErrorMessage { get; protected set; }
    public string SubmitLabel { get; protected set; } = "Save";

    protected bool PrepareInput()
    {
        Input.Name = Input.Name.Trim(); Input.Type = Input.Type.Trim();
        if (string.IsNullOrWhiteSpace(Input.Name)) ModelState.AddModelError("Input.Name", "Name is required.");
        if (string.IsNullOrWhiteSpace(Input.Type)) ModelState.AddModelError("Input.Type", "Type is required.");
        if (Input.MinimumTemperature > Input.MaximumTemperature) ModelState.AddModelError("Input.MinimumTemperature", "Minimum temperature cannot exceed maximum temperature.");
        var ids = new List<int>();
        foreach (var value in IngredientIdsText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (!int.TryParse(value, out var id) || id <= 0) ModelState.AddModelError(nameof(IngredientIdsText), "Ingredient IDs must be positive whole numbers."); else ids.Add(id);
        Input.IngredientIds = ids;
        return ModelState.IsValid;
    }

    protected void SetInput(StorageLocationResponse location)
    {
        Input = new StorageLocationRequest { Name = location.Name, Type = location.Type, MinimumTemperature = location.MinimumTemperature, MaximumTemperature = location.MaximumTemperature, IngredientIds = location.Ingredients.Select(i => i.Id).ToList() };
        IngredientIdsText = string.Join(", ", Input.IngredientIds);
    }
}