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

public sealed class IngredientApiSecurityTests : IDisposable
{
    private const string Issuer = "https://issuer.test/realms/ctrl-eat";
    private const string Audience = "ctrleat-ingredients";
    private static readonly SymmetricSecurityKey Key = new(Encoding.UTF8.GetBytes(
        "test-only-ingredient-signing-key-never-used-in-production-0123456789"));

    private readonly string _database = Path.Combine(Path.GetTempPath(),
        $"ctrleat-ingredient-api-{Guid.NewGuid():N}.db");
    private readonly WebApplicationFactory<Program> _factory;

    public IngredientApiSecurityTests()
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
                    ["ConnectionStrings:Ingredient"] = $"Data Source={_database};Pooling=False"
                }));
            builder.ConfigureTestServices(services => services.Configure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = Issuer;
                    options.RequireHttpsMetadata = true;
                    options.TokenValidationParameters.ValidIssuer = Issuer;
                    options.TokenValidationParameters.ValidAudience = Audience;
                    var metadata = new OpenIdConnectConfiguration { Issuer = Issuer };
                    metadata.SigningKeys.Add(Key);
                     options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
                }));
        });
    }

    private static string Token(string scopes, string audience = Audience,
        bool expired = false, bool invalidSignature = false)
    {
        var key = invalidSignature
            ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                "different-test-signing-key-never-used-in-production-0123456789"))
            : Key;
        var token = new JwtSecurityToken(
            issuer: Issuer, audience: audience,
            claims: new[] { new Claim("sub", "test-user"), new Claim("scope", scopes) },
            notBefore: DateTime.UtcNow.AddMinutes(-20),
            expires: DateTime.UtcNow.AddMinutes(expired ? -1 : 10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
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
    public async Task Health_is_public_but_ingredient_routes_require_a_token()
    {
        using var client = Client();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/Ingredients/1")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/Ingredients/",
            new { Name = "Rice", FoodGroup = "Grain", Calories = 100 })).StatusCode);
    }

    [Fact]
    public async Task Valid_signature_but_wrong_scope_gives_403()
    {
        using var client = Client(Token("ingredients.write"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/Ingredients/1")).StatusCode);
    }

    [Theory]
    [InlineData("wrong-audience", false, false)]
    [InlineData(Audience, true, false)]
    [InlineData(Audience, false, true)]
    public async Task Wrong_audience_expiration_or_signature_gives_401(
        string audience, bool expired, bool invalidSignature)
    {
        using var client = Client(Token("ingredients.read", audience, expired, invalidSignature));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/Ingredients/1")).StatusCode);
    }

    [Fact]
    public async Task Authorized_create_read_and_invalid_input()
    {
        using var writeClient = Client(Token("ingredients.write"));
        var invalid = await writeClient.PostAsJsonAsync("/api/Ingredients/",
            new { Name = "", FoodGroup = "Grain", Calories = -1 });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var created = await writeClient.PostAsJsonAsync("/api/Ingredients/",
            new { Name = "Rice", FoodGroup = "Grain", Calories = 100 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var readClient = Client(Token("ingredients.read"));
        var rows = await readClient.GetFromJsonAsync<List<IngredientItem>>("/api/Ingredients/");
        Assert.NotNull(rows);
        Assert.Single(rows);
        Assert.Equal("Rice", rows[0].Name);
        Assert.Equal(100m, rows[0].Calories);
    }

    private sealed record IngredientItem(int Id, string Name, string FoodGroup, decimal Calories);

    public void Dispose()
    {
        _factory.Dispose();
        foreach (var suffix in new[] { "", "-shm", "-wal" })
        {
            try { File.Delete(_database + suffix); } catch (IOException) { }
        }
    }
}
