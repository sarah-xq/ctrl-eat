using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace DailyMealPlan.IntegrationTests;

public sealed class MealPlanApiTests : IDisposable
{
    private const string Issuer = "https://issuer.example.test";
    private const string Audience = "ctrleat-mealplans";
    private readonly string database = Path.Combine(Path.GetTempPath(), $"ctrleat-{Guid.NewGuid():N}.db");
    private readonly RSA signingKey = RSA.Create(2048);

    private WebApplicationFactory<Program> OpenApi() => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DailyMealPlan", $"Data Source={database};Pooling=False");
            builder.UseSetting("Authentication:Authority", Issuer);
            builder.UseSetting("Authentication:Audience", Audience);
            builder.ConfigureTestServices(services =>
            {
                services.ConfigureHttpClientDefaults(client => client.ConfigurePrimaryHttpMessageHandler(() => new LookupHandler()));
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    var discovery = new OpenIdConnectConfiguration { Issuer = Issuer };
                    discovery.SigningKeys.Add(new RsaSecurityKey(signingKey) { KeyId = "test" });
                    options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(discovery);
                });
            });
        });

    private string Token(string subject = "alice", string scope = "daily-meal-plans.read daily-meal-plans.write",
        string issuer = Issuer, string audience = Audience, bool expired = false, RSA? otherKey = null)
    {
        var now = DateTime.UtcNow;
        var key = new RsaSecurityKey(otherKey ?? signingKey) { KeyId = "test" };
        var jwt = new JwtSecurityToken(issuer, audience,
            [new Claim("sub", subject), new Claim("scope", scope)], now.AddHours(-2),
            expired ? now.AddHours(-1) : now.AddMinutes(10), new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static void UseToken(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private const string Plans = "/api/daily-meal-plans";
    private static readonly object EmptyPlan = new { date = "2026-11-05", snacks = Array.Empty<object>() };

    private sealed class LookupHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var response = request.RequestUri!.AbsolutePath switch
            {
                "/api/recipes/1" => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { id = 1, name = "Soup", totalCalories = 40 }) },
                "/api/ingredients/1" => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { id = 1, name = "Rice", calories = 40 }) },
                "/api/recipes/503" => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            };
            return Task.FromResult(response);
        }
    }

    [Fact]
    public async Task HealthTokenValidationAndSeparateScopes()
    {
        using var host = OpenApi();
        using var client = host.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Plans)).StatusCode);
        using var forged = RSA.Create(2048);
        foreach (var token in new[] { "garbage", Token(expired: true), Token(issuer: "https://impostor.test"),
            Token(audience: "ctrleat-users"), Token(otherKey: forged) })
        {
            UseToken(client, token);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Plans)).StatusCode);
        }
        UseToken(client, Token(scope: "daily-meal-plans.read"));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Plans)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(Plans, EmptyPlan)).StatusCode);
        UseToken(client, Token(scope: "daily-meal-plans.write"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Plans)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(Plans, EmptyPlan)).StatusCode);
    }

    [Fact]
    public async Task OwnerAndSavedCaloriesSurviveLookupFailuresAndRestart()
    {
        string item;
        using (var host = OpenApi())
        using (var client = host.CreateClient())
        {
            UseToken(client, Token());
            var creation = await client.PostAsJsonAsync(Plans, new {
                date = "2026-11-05", userId = "bob", breakfastRecipeId = 1, lunchRecipeId = 1, dinnerRecipeId = 1,
                snacks = new[] { new { isRecipe = true, itemId = 1, quantity = 0.5m, unit = "serving" },
                                new { isRecipe = false, itemId = 1, quantity = 2m, unit = "cup" } }
            });
            Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
            var created = await creation.Content.ReadFromJsonAsync<JsonElement>();
            item = Plans + "/" + created.GetProperty("id").GetInt32();
            Assert.Equal(220m, created.GetProperty("totalCalories").GetDecimal());
            var invalid = await client.PutAsJsonAsync(item, new { date = "2026-11-06", lunchRecipeId = 999 });
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            Assert.Contains("lunch", await invalid.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PutAsJsonAsync(item, new { date = "2026-11-06", dinnerRecipeId = 503 })).StatusCode);
            UseToken(client, Token("bob"));
            Assert.Empty((await client.GetFromJsonAsync<JsonElement[]>(Plans + "?date=2026-11-05"))!);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(item)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync(item, EmptyPlan)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync(item)).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(Plans, EmptyPlan)).StatusCode);
        }
        using var restarted = OpenApi();
        using var reader = restarted.CreateClient();
        UseToken(reader, Token());
        var persisted = Assert.Single((await reader.GetFromJsonAsync<JsonElement[]>(Plans + "?date=2026-11-05"))!);
        Assert.Equal(220m, persisted.GetProperty("totalCalories").GetDecimal());
        Assert.Empty((await reader.GetFromJsonAsync<JsonElement[]>(Plans + "?date=2026-11-06"))!);
        (await reader.PutAsJsonAsync(item, EmptyPlan)).EnsureSuccessStatusCode();
        var cleared = await reader.GetFromJsonAsync<JsonElement>(item);
        Assert.Equal(0m, cleared.GetProperty("totalCalories").GetDecimal());
        Assert.Empty(cleared.GetProperty("snacks").EnumerateArray());
        Assert.Equal(HttpStatusCode.NoContent, (await reader.DeleteAsync(item)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await reader.GetAsync(item)).StatusCode);
    }

    public void Dispose()
    {
        signingKey.Dispose();
        File.Delete(database);
    }
}
