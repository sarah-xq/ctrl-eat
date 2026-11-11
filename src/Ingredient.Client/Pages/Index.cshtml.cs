using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ingredient.Client.Pages;

public sealed class IndexModel(IHttpClientFactory factory) : PageModel
{
    [BindProperty] public int Id { get; set; }
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public string FoodGroup { get; set; } = "";
    [BindProperty] public decimal Calories { get; set; }
    [BindProperty] public DateTime? ExpiryDate { get; set; }
    [BindProperty] public string StorageMethod { get; set; } = "RoomTemperature";
    [BindProperty] public string? BatchNumber { get; set; }
    [BindProperty] public int AlertThresholdDays { get; set; } = 7;
    [BindProperty] public decimal ProteinGrams { get; set; }
    [BindProperty] public decimal CarbohydratesGrams { get; set; }
    [BindProperty] public decimal FatGrams { get; set; }
    [BindProperty] public decimal FiberGrams { get; set; }
    [BindProperty] public decimal SugarGrams { get; set; }
    [BindProperty] public decimal SodiumMilligrams { get; set; }
    [BindProperty] public decimal CholesterolMilligrams { get; set; }
    [BindProperty] public string ServingSize { get; set; } = "1 serving";
    [BindProperty] public string? DataSource { get; set; }
    [BindProperty] public DateTime? SourceDate { get; set; }
    [BindProperty] public string VitaminsMineralsText { get; set; } = "";
    [BindProperty] public string AllergensText { get; set; } = "";

    public List<IngredientView> Items { get; private set; } = new();
    public List<ExpiryAlertView> Alerts { get; private set; } = new();
    public string? Error { get; private set; }

    private async Task<HttpClient> CreateClientAsync()
    {
        var client = factory.CreateClient("Api");
        var token = await HttpContext.GetTokenAsync("access_token");
        if (token is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task OnGetAsync()
    {
        var client = await CreateClientAsync();
        var response = await client.GetAsync("api/Ingredients");
        if (response.IsSuccessStatusCode)
            Items = await response.Content.ReadFromJsonAsync<List<IngredientView>>() ?? new();
        else Error ??= $"Ingredient API returned HTTP {(int)response.StatusCode}.";
        var alertsResponse = await client.GetAsync("api/Ingredients/alerts");
        if (alertsResponse.IsSuccessStatusCode)
            Alerts = await alertsResponse.Content.ReadFromJsonAsync<List<ExpiryAlertView>>() ?? new();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var allergens = new List<object>();
        var vitamins = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in AllergensText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var segments = part.Split(':', 2, StringSplitOptions.TrimEntries);
            if (segments.Length != 2 || !new[] { "Contains", "MayContain", "Trace" }.Contains(segments[1], StringComparer.OrdinalIgnoreCase))
            { Error = "Allergens must use Name:Contains, Name:MayContain or Name:Trace."; await OnGetAsync(); return Page(); }
            var presence = Array.FindIndex(new[] { "Contains", "MayContain", "Trace" },
                value => value.Equals(segments[1], StringComparison.OrdinalIgnoreCase));
            allergens.Add(new { Name = segments[0], Presence = presence });
        }
        foreach (var part in VitaminsMineralsText.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pieces = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (pieces.Length != 2 || !decimal.TryParse(pieces[1], out var value) || value < 0 || !vitamins.TryAdd(pieces[0], value))
            { Error = "Vitamins/minerals must use VitaminC=10;Iron=4 format with nonnegative numbers."; await OnGetAsync(); return Page(); }
        }
        var body = new
        {
            Name, FoodGroup, Calories, ExpiryDate, StorageMethod, BatchNumber, AlertThresholdDays,
            Nutrition = new { ProteinGrams, CarbohydratesGrams, FatGrams, FiberGrams, SugarGrams,
                SodiumMilligrams, CholesterolMilligrams, ServingSize, DataSource, SourceDate,
                VitaminsMinerals = vitamins },
            Allergens = allergens
        };
        var client = await CreateClientAsync();
        var response = Id > 0 ? await client.PutAsJsonAsync($"api/Ingredients/{Id}", body)
            : await client.PostAsJsonAsync("api/Ingredients", body);
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
        var response = await (await CreateClientAsync()).DeleteAsync($"api/Ingredients/{id}");
        if (!response.IsSuccessStatusCode) { Error = await response.Content.ReadAsStringAsync(); await OnGetAsync(); return Page(); }
        return RedirectToPage();
    }

    public sealed record IngredientView(int Id, string Name, string FoodGroup, decimal Calories,
        DateTime? ExpiryDate, string StorageMethod, string? BatchNumber, int AlertThresholdDays,
        int? DaysUntilExpiry, int ExpiryStatus, NutritionView? Nutrition, List<AllergenView>? Allergens);
    public sealed record NutritionView(decimal ProteinGrams, decimal CarbohydratesGrams, decimal FatGrams,
        decimal FiberGrams, decimal SugarGrams, decimal SodiumMilligrams, decimal CholesterolMilligrams);
    public sealed record AllergenView(string Name, int Presence);
    public sealed record ExpiryAlertView(int IngredientId, string Name, int DaysUntilExpiry, int Status);
}
