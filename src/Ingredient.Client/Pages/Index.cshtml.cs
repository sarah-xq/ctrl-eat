using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ingredient.Client.Pages;
public sealed class IndexModel(IHttpClientFactory factory) : PageModel
{
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public string FoodGroup { get; set; } = "";
    [BindProperty] public decimal Calories { get; set; }
    [BindProperty] public int Id { get; set; }
    public List<IngredientView> Items { get; private set; } = new();
    public string? Error { get; private set; }
    private async Task<HttpClient> CreateClientAsync()
    {
        var token = await HttpContext.GetTokenAsync("access_token");
        var client = factory.CreateClient("Api");
        if (token is not null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
    public async Task OnGetAsync()
    {
        var client = await CreateClientAsync();
        var response = await client.GetAsync("api/Ingredients");
        if (response.IsSuccessStatusCode) Items = await response.Content.ReadFromJsonAsync<List<IngredientView>>() ?? new();
        else Error = $"Ingredient API returned HTTP {(int)response.StatusCode}. Check scope and API configuration.";
    }
    public async Task<IActionResult> OnPostSaveAsync()
    {
        var client = await CreateClientAsync();
        var body = new { Name, FoodGroup, Calories, ExpiryDate = (DateTime?)null };
        var response = Id > 0 ? await client.PutAsJsonAsync($"api/Ingredients/{Id}", body)
            : await client.PostAsJsonAsync("api/Ingredients", body);
        if (!response.IsSuccessStatusCode) { Error = await response.Content.ReadAsStringAsync(); await OnGetAsync(); return Page(); }
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var response = await (await CreateClientAsync()).DeleteAsync($"api/Ingredients/{id}");
        if (!response.IsSuccessStatusCode) { Error = await response.Content.ReadAsStringAsync(); await OnGetAsync(); return Page(); }
        return RedirectToPage();
    }
    public sealed record IngredientView(int Id, string Name, string FoodGroup, decimal Calories, DateTime? ExpiryDate);
}
