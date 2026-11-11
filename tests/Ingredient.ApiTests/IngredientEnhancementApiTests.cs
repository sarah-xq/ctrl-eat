using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Ingredient.ApiTests;

public sealed class IngredientEnhancementApiTests : IDisposable
{
    private const string Issuer = "https://issuer.test/realms/ctrl-eat";
    private static readonly SymmetricSecurityKey Key = new(Encoding.UTF8.GetBytes(
        "test-only-ingredient-enhancements-signing-key-0123456789"));
    private readonly string _db = Path.Combine(Path.GetTempPath(), $"ingredient-enhanced-{Guid.NewGuid():N}.db");
    private readonly WebApplicationFactory<Program> _factory;

    public IngredientEnhancementApiTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Authentication:Authority", Issuer);
            builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authority"] = Issuer,
                ["Authentication:Audience"] = "ctrleat-ingredients",
                ["ConnectionStrings:Ingredient"] = $"Data Source={_db};Pooling=False"
            }));
            builder.ConfigureTestServices(services => services.Configure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = Issuer;
                    options.RequireHttpsMetadata = true;
                    var config = new OpenIdConnectConfiguration { Issuer = Issuer };
                    config.SigningKeys.Add(Key);
                    options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(config);
                }));
        });
    }

    private HttpClient Client(string scopes)
    {
        var token = new JwtSecurityToken(Issuer, "ctrleat-ingredients",
            new[] { new Claim("sub", "nutrition-test"), new Claim("scope", scopes) },
            DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(5),
            new SigningCredentials(Key, SecurityAlgorithms.HmacSha256));
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }

    [Fact]
    public async Task Nutrition_allergens_expiry_and_migrations_roundtrip()
    {
        using var client = Client("ingredients.read ingredients.write");
        var created = await client.PostAsJsonAsync("/api/Ingredients/", new
        {
            Name = "Peanut milk", FoodGroup = "Dairy", Calories = 100m,
            ExpiryDate = DateTime.UtcNow.Date.AddDays(2), AlertThresholdDays = 5,
            StorageMethod = "Refrigerated", BatchNumber = "L-12",
            Nutrition = new { ProteinGrams = 4m, CarbohydratesGrams = 6m, FatGrams = 2m,
                FiberGrams = 1m, SugarGrams = 3m, SodiumMilligrams = 25m,
                CholesterolMilligrams = 0m, ServingSize = "200mL",
                VitaminsMinerals = new Dictionary<string, decimal> { ["VitaminC"] = 12m } },
            Allergens = new[] { new { Name = "Peanuts", Presence = 0 }, new { Name = "Milk", Presence = 1 } }
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var body = await System.Text.Json.JsonDocument.ParseAsync(await created.Content.ReadAsStreamAsync());
        var row = body.RootElement;
        Assert.Equal(4, row.GetProperty("nutrition").GetProperty("proteinGrams").GetDecimal());
        Assert.Equal(2, row.GetProperty("allergens").GetArrayLength());
        Assert.Equal("Refrigerated", row.GetProperty("storageMethod").GetString());
        var alerts = await client.GetFromJsonAsync<List<System.Text.Json.JsonElement>>("/api/Ingredients/alerts");
        Assert.NotNull(alerts);
        Assert.Single(alerts);
        var id = row.GetProperty("id").GetInt32();
        var deleted = await client.DeleteAsync($"/api/Ingredients/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Empty(await client.GetFromJsonAsync<List<System.Text.Json.JsonElement>>("/api/Ingredients/alerts") ?? []);
    }

    [Fact]
    public async Task Invalid_nutrition_and_duplicate_allergens_are_rejected()
    {
        using var client = Client("ingredients.read ingredients.write");
        var negative = await client.PostAsJsonAsync("/api/Ingredients/", new
        {
            Name = "Milk", FoodGroup = "Dairy", Calories = 20m,
            Nutrition = new { ProteinGrams = -1m }
        });
        Assert.Equal(HttpStatusCode.BadRequest, negative.StatusCode);
        var duplicate = await client.PostAsJsonAsync("/api/Ingredients/", new
        {
            Name = "Milk", FoodGroup = "Dairy", Calories = 20m,
            Allergens = new[] { new { Name = "Milk", Presence = 0 }, new { Name = "milk", Presence = 2 } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
    }

    public void Dispose()
    {
        _factory.Dispose();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
            try { File.Delete(_db + suffix); } catch (IOException) { }
    }
}
