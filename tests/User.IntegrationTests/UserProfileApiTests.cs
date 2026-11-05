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

namespace User.IntegrationTests;

public sealed class UserProfileApiTests : IDisposable
{
    private const string Issuer = "https://issuer.example.test";
    private const string Audience = "ctrleat-users";
    private readonly string database = Path.Combine(Path.GetTempPath(), $"ctrleat-{Guid.NewGuid():N}.db");
    private readonly RSA signingKey = RSA.Create(2048);

    private WebApplicationFactory<Program> OpenApi() => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:User", $"Data Source={database};Pooling=False");
            builder.UseSetting("Authentication:Authority", Issuer);
            builder.UseSetting("Authentication:Audience", Audience);
            builder.ConfigureTestServices(services =>
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    var discovery = new OpenIdConnectConfiguration { Issuer = Issuer };
                    discovery.SigningKeys.Add(new RsaSecurityKey(signingKey) { KeyId = "test" });
                    options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(discovery);
                }));
        });

    private string Token(string subject = "alice", string scope = "users.read users.write",
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

    private static async Task<int?> Goal(HttpClient client)
    {
        using var response = await client.GetAsync("/api/users/me");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("calorieGoal", Assert.Single(json.EnumerateObject()).Name);
        return json.GetProperty("calorieGoal").Deserialize<int?>();
    }

    [Fact]
    public async Task PublicHealthAndInvalidBearerRequests()
    {
        using var host = OpenApi();
        using var client = host.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
        using var forged = RSA.Create(2048);
        foreach (var token in new[] { "garbage", Token(expired: true), Token(issuer: "https://impostor.test"),
            Token(audience: "elsewhere"), Token(otherKey: forged) })
        {
            UseToken(client, token);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
        }
    }

    [Fact]
    public async Task ReadAndWritePermissionsAreIndependent()
    {
        using var host = OpenApi();
        using var client = host.CreateClient();
        UseToken(client, Token(scope: "users.read"));
        Assert.Null(await Goal(client));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/api/users/me/calorie-goal", new { calorieGoal = 2000 })).StatusCode);
        UseToken(client, Token(scope: "users.write"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/users/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/users/me/calorie-goal", new { calorieGoal = 2000 })).StatusCode);
    }

    [Fact]
    public async Task InvalidEditsKeepSavedValueAndUsersRemainIsolatedAfterRestart()
    {
        using (var host = OpenApi())
        using (var client = host.CreateClient())
        {
            UseToken(client, Token());
            Assert.Null(await Goal(client));
            (await client.PutAsJsonAsync("/api/users/me/calorie-goal", new { calorieGoal = 2100 })).EnsureSuccessStatusCode();
            foreach (var json in new[] { "{}", "{", "{\"calorieGoal\":null}", "{\"calorieGoal\":0}",
                "{\"calorieGoal\":-1}", "{\"calorieGoal\":2.5}", "{\"calorieGoal\":2147483648}" })
            {
                using var body = new StringContent(json, Encoding.UTF8, "application/json");
                Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsync("/api/users/me/calorie-goal", body)).StatusCode);
            }
            Assert.Equal(2100, await Goal(client));
            UseToken(client, Token(subject: "bob"));
            client.DefaultRequestHeaders.Add("X-User-Id", "alice");
            Assert.Null(await Goal(client));
            (await client.PutAsJsonAsync("/api/users/me/calorie-goal", new { calorieGoal = 1800 })).EnsureSuccessStatusCode();
        }
        using var restarted = OpenApi();
        using var reader = restarted.CreateClient();
        UseToken(reader, Token());
        Assert.Equal(2100, await Goal(reader));
        UseToken(reader, Token(subject: "bob"));
        Assert.Equal(1800, await Goal(reader));
    }

    public void Dispose()
    {
        signingKey.Dispose();
        File.Delete(database);
    }
}
