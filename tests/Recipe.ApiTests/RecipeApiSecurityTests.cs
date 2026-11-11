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

public sealed class RecipeApiSecurityTests : IDisposable
{
    private const string Issuer = "https://issuer.test/realms/ctrl-eat";
    private const string Audience = "ctrleat-recipes";
    private static readonly SymmetricSecurityKey Key = new(Encoding.UTF8.GetBytes(
        "test-only-recipe-signing-key-never-used-in-production-0123456789"));
    private readonly string _database = Path.Combine(Path.GetTempPath(),
        $"ctrleat-recipe-api-{Guid.NewGuid():N}.db");
    private readonly WebApplicationFactory<Program> _factory;

    public RecipeApiSecurityTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Authentication:Authority", Issuer);
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:Authority"] = Issuer,
                    ["Authentication:Audience"] = Audience,
                    ["ConnectionStrings:Recipe"] = $"Data Source={_database};Pooling=False",
                    ["IngredientApi:BaseUrl"] = "https://ingredient.test/"
                }));
            builder.ConfigureTestServices(services =>
            {
                services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = Issuer;
                    options.RequireHttpsMetadata = true;
                    options.TokenValidationParameters.ValidIssuer = Issuer;
                    options.TokenValidationParameters.ValidAudience = Audience;
                    var metadata = new OpenIdConnectConfiguration { Issuer = Issuer };
                    metadata.SigningKeys.Add(Key);
                     options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
                });
                services.PostConfigure<HttpClientFactoryOptions>("IngredientApi", options =>
                    options.HttpMessageHandlerBuilderActions.Add(handler =>
                        handler.PrimaryHandler = new IngredientContractHandler()));
            });
        });
    }

    private static string Token(string scopes, bool includeIngredientAudience = false,
        string audience = Audience)
    {
        var claims = new List<Claim> { new("sub", "test-user"), new("scope", scopes) };
        if (includeIngredientAudience) claims.Add(new Claim("aud", "ctrleat-ingredients"));
        var token = new JwtSecurityToken(
            issuer: Issuer, audience: audience, claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1), expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(Key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private HttpClient Client(string? token = null)
    {
        var client = _factory.CreateClient();
        if (token is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Recipe_requires_authentication_and_correct_scope()
    {
        using var guest = Client();
        Assert.Equal(HttpStatusCode.OK, (await guest.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync("/api/recipes/")).StatusCode);
        using var writeOnly = Client(Token("recipes.write"));
        Assert.Equal(HttpStatusCode.Forbidden, (await writeOnly.GetAsync("/api/recipes/")).StatusCode);
        using var wrongAudience = Client(Token("recipes.read", audience: "some-other-api"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await wrongAudience.GetAsync("/api/recipes/")).StatusCode);
    }

    [Fact]
    public async Task Recipe_forwards_dual_audience_scoped_token_and_calculates_calories()
    {
        using var client = Client(Token("recipes.read recipes.write ingredients.read", includeIngredientAudience: true));
        var created = await client.PostAsJsonAsync("/api/recipes/", new
        {
            Name = "Rice bowl", TotalTimeMinutes = 20, CookTimeMinutes = 15,
            Description = "Sample", Ingredients = new[] { new { IngredientId = 7, Quantity = 2m, Unit = "serving" } }
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var list = await client.GetFromJsonAsync<List<RecipeItem>>("/api/recipes/");
        Assert.NotNull(list);
        Assert.Single(list);
        Assert.Equal("Rice bowl", list[0].Name);
        Assert.Equal(200m, list[0].TotalCalories);
    }

    [Fact]
    public async Task Ingredient_lookup_rejects_missing_ingredient_scope()
    {
        using var client = Client(Token("recipes.write", includeIngredientAudience: true));
        var response = await client.PostAsJsonAsync("/api/recipes/", new
        {
            Name = "Rice bowl", TotalTimeMinutes = 20, CookTimeMinutes = 15,
            Ingredients = new[] { new { IngredientId = 7, Quantity = 2m, Unit = "serving" } }
        });
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task Missing_ingredient_returns_400_without_saving_recipe()
    {
        using var client = Client(Token("recipes.read recipes.write ingredients.read", includeIngredientAudience: true));
        var response = await client.PostAsJsonAsync("/api/recipes/", new
        {
            Name = "Rice bowl", TotalTimeMinutes = 20, CookTimeMinutes = 15,
            Ingredients = new[] { new { IngredientId = 999, Quantity = 2m, Unit = "serving" } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var list = await client.GetFromJsonAsync<List<RecipeItem>>("/api/recipes/");
        Assert.NotNull(list);
        Assert.Empty(list);
    }

    private sealed record RecipeItem(int Id, string Name, decimal TotalCalories);

    private sealed class IngredientContractHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var auth = request.Headers.Authorization;
            if (auth?.Scheme != "Bearer" || string.IsNullOrWhiteSpace(auth.Parameter))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            JwtSecurityToken jwt;
            try { jwt = new JwtSecurityTokenHandler().ReadJwtToken(auth.Parameter); }
            catch (Exception) { return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)); }
            if (!jwt.Audiences.Contains("ctrleat-ingredients"))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            if (!jwt.Claims.Where(c => c.Type == "scope")
                    .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    .Contains("ingredients.read"))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden));
            if (request.RequestUri?.AbsolutePath != "/api/Ingredients/7")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { Id = 7, Name = "Rice", FoodGroup = "Grain", Calories = 100m })
            });
        }
    }

    public void Dispose()
    {
        _factory.Dispose();
        foreach (var suffix in new[] { "", "-shm", "-wal" })
        {
            try { File.Delete(_database + suffix); } catch (IOException) { }
        }
    }
}
