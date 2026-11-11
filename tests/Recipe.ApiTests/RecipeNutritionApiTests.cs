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
using Microsoft.Extensions.Http;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Recipe.ApiTests;

public sealed class RecipeNutritionApiTests : IDisposable
{
    private const string Issuer = "https://issuer.test/realms/ctrl-eat";
    private static readonly SymmetricSecurityKey Key = new(Encoding.UTF8.GetBytes(
        "test-only-recipe-nutrition-signing-key-0123456789"));
    private readonly string _db = Path.Combine(Path.GetTempPath(), $"recipe-nutrition-{Guid.NewGuid():N}.db");
    private readonly WebApplicationFactory<Program> _factory;

    public RecipeNutritionApiTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Authentication:Authority", Issuer);
            builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authority"] = Issuer,
                ["Authentication:Audience"] = "ctrleat-recipes",
                ["ConnectionStrings:Recipe"] = $"Data Source={_db};Pooling=False",
                ["IngredientApi:BaseUrl"] = "https://ingredient.test/"
            }));
            builder.ConfigureTestServices(services =>
            {
                services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = Issuer;
                    options.RequireHttpsMetadata = true;
                    var config = new OpenIdConnectConfiguration { Issuer = Issuer };
                    config.SigningKeys.Add(Key);
                    options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(config);
                });
                services.PostConfigure<HttpClientFactoryOptions>("IngredientApi", options =>
                    options.HttpMessageHandlerBuilderActions.Add(builder => builder.PrimaryHandler = new StubIngredients()));
            });
        });
    }

    [Fact]
    public async Task Recipe_aggregates_nutrients_vitamins_and_allergens()
    {
        var token = new JwtSecurityToken(Issuer, "ctrleat-recipes",
            new[] { new Claim("sub", "nutrition-test"), new Claim("scope", "recipes.write recipes.read ingredients.read"),
                new Claim("aud", "ctrleat-ingredients") },
            DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(5),
            new SigningCredentials(Key, SecurityAlgorithms.HmacSha256));
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        var created = await client.PostAsJsonAsync("/api/recipes/", new
        {
            Name = "Meal", TotalTimeMinutes = 20, CookTimeMinutes = 10,
            Ingredients = new[] { new { IngredientId = 7, Quantity = 2m, Unit = "serving" } }
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var row = await created.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(200m, row.GetProperty("totalCalories").GetDecimal());
        Assert.Equal(8m, row.GetProperty("nutrition").GetProperty("proteinGrams").GetDecimal());
        Assert.Equal(24m, row.GetProperty("vitaminsMinerals").GetProperty("VitaminC").GetDecimal());
        Assert.Single(row.GetProperty("allergens").EnumerateArray());
    }

    private sealed class StubIngredients : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.Headers.Authorization?.Scheme != "Bearer")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    Id = 7, Name = "Milk", FoodGroup = "Dairy", Calories = 100m,
                    Nutrition = new { ProteinGrams = 4m,
                        VitaminsMinerals = new Dictionary<string, decimal> { ["VitaminC"] = 12m } },
                    Allergens = new[] { new { Name = "milk", Presence = 0 } }
                })
            });
        }
    }

    public void Dispose()
    {
        _factory.Dispose();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
            try { File.Delete(_db + suffix); } catch (IOException) { }
    }
}
