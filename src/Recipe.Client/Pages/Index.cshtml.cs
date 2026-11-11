using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Recipe.Client.Pages;

public sealed class IndexModel(IHttpClientFactory factory) : PageModel
{
    [BindProperty] public int Id { get; set; }
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public string? Author { get; set; }
    [BindProperty] public string Description { get; set; } = "";
    [BindProperty] public int TotalTimeMinutes { get; set; }
    [BindProperty] public int CookTimeMinutes { get; set; }
    [BindProperty] public string IngredientsText { get; set; } = "";
    public List<RecipeView> Items { get; private set; } = new();
    public string? Error { get; private set; }

    private async Task<HttpClient> CreateClientAsync()
    {
        var client = factory.CreateClient("Api");
        var token = await HttpContext.GetTokenAsync("access_token");
        if (token is not null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task OnGetAsync()
    {
        var response = await (await CreateClientAsync()).GetAsync("api/recipes");
        if (response.IsSuccessStatusCode)
            Items = await response.Content.ReadFromJsonAsync<List<RecipeView>>() ?? new();
        else Error ??= $"Recipe API returned HTTP {(int)response.StatusCode}. Check scope and API configuration.";
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var ingredients = new List<object>();
        foreach (var item in IngredientsText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = item.Split(':', 3, StringSplitOptions.TrimEntries);
            if (parts.Length != 3 || !int.TryParse(parts[0], out var id) || id <= 0 ||
                !decimal.TryParse(parts[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity) ||
                quantity <= 0 || string.IsNullOrWhiteSpace(parts[2]))
            {
                Error = "Ingredients must use ID:Quantity:Unit, for example 7:2:serving,8:1:cup.";
                await OnGetAsync();
                return Page();
            }
            ingredients.Add(new { IngredientId = id, Quantity = quantity, Unit = parts[2] });
        }
        var body = new { Name, Author, Description, TotalTimeMinutes, CookTimeMinutes, Ingredients = ingredients };
        var client = await CreateClientAsync();
        var response = Id > 0 ? await client.PutAsJsonAsync($"api/recipes/{Id}", body)
            : await client.PostAsJsonAsync("api/recipes", body);
        if (!response.IsSuccessStatusCode)
        {
            Error = await response.Content.ReadAsStringAsync();
            await OnGetAsync();
            return Page();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var response = await (await CreateClientAsync()).DeleteAsync($"api/recipes/{id}");
        if (!response.IsSuccessStatusCode)
        {
            Error = await response.Content.ReadAsStringAsync();
            await OnGetAsync();
            return Page();
        }
        return RedirectToPage();
    }

    public sealed record RecipeView(int Id, string Name, string? Author, decimal TotalCalories,
        NutritionView? Nutrition, List<AllergenView>? Allergens);
    public sealed record NutritionView(decimal ProteinGrams, decimal CarbohydratesGrams, decimal FatGrams);
    public sealed record AllergenView(string Name, int Presence);
}
