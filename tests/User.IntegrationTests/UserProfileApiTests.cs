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
using Microsoft.Extensions.DependencyInjection.Extensions;
using User.Data;
using User.Domain.Entities;

namespace User.IntegrationTests;

public sealed class UserProfileApiTests : IDisposable
{
    private const string Issuer = "https://issuer.example.test";
    private const string Audience = "ctrleat-users";
    private readonly string database = Path.Combine(Path.GetTempPath(), $"ctrleat-{Guid.NewGuid():N}.db");
    private readonly RSA signingKey = RSA.Create(2048);
    private readonly string pictureStorage = Path.Combine(
        Path.GetTempPath(),
        $"ctrleat-pictures-{Guid.NewGuid():N}");

    private WebApplicationFactory<Program> OpenApi() => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:User", $"Data Source={database};Pooling=False");
            builder.UseSetting("Authentication:Authority", Issuer);
            builder.UseSetting("Authentication:Audience", Audience);
            builder.ConfigureTestServices(services =>
            {
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    var discovery = new OpenIdConnectConfiguration { Issuer = Issuer };
                    discovery.SigningKeys.Add(new RsaSecurityKey(signingKey) { KeyId = "test" });
                    options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(discovery);
                });
                services.RemoveAll<IProfilePictureStorage>();
                services.AddSingleton<IProfilePictureStorage>(
                    new LocalProfilePictureStorage(pictureStorage));
            });
        });

    private string Token(string subject = "alice", string scope = "users.read users.write",
        string issuer = Issuer, string audience = Audience, bool expired = false, RSA? otherKey = null,
        bool includeSubject = true)
    {
        var now = DateTime.UtcNow;
        var key = new RsaSecurityKey(otherKey ?? signingKey) { KeyId = "test" };
        var claims = includeSubject
            ? new[] { new Claim("sub", subject), new Claim("scope", scope) }
            : new[] { new Claim("scope", scope) };
        var jwt = new JwtSecurityToken(issuer, audience,
            claims, now.AddHours(-2),
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
            Token(audience: "elsewhere"), Token(otherKey: forged), Token(includeSubject: false) })
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

    [Fact]
    public async Task ProfilePictureUploadAndPrivateReadUseAuthenticatedAccount()
    {
        using var host = OpenApi();
        using var client = host.CreateClient();
        UseToken(client, Token(subject: "alice"));
        var jpeg = new byte[] { 0xFF, 0xD8, 0x01, 0x02, 0xFF, 0xD9 };
        using var upload = new MultipartFormDataContent();
        var file = new ByteArrayContent(jpeg);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        upload.Add(file, "file", "profile.jpg");

        using var response = await client.PutAsync("/api/users/me/profile-picture", upload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Jpeg", saved.GetProperty("format").GetString());
        Assert.Equal(jpeg.Length, saved.GetProperty("sizeBytes").GetInt64());

        using var image = await client.GetAsync("/api/users/me/profile-picture");
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/jpeg", image.Content.Headers.ContentType?.MediaType);
        Assert.Equal(jpeg, await image.Content.ReadAsByteArrayAsync());

        UseToken(client, Token(subject: "bob"));
        using var otherImage = await client.GetAsync("/api/users/me/profile-picture");
        Assert.Equal(HttpStatusCode.NotFound, otherImage.StatusCode);
    }

    [Fact]
    public async Task InvalidProfilePictureDoesNotChangeSavedImage()
    {
        using var host = OpenApi();
        using var client = host.CreateClient();
        UseToken(client, Token(subject: "alice"));

        var valid = new byte[] { 0xFF, 0xD8, 0x01, 0x02, 0xFF, 0xD9 };
        using (var validUpload = new MultipartFormDataContent())
        {
            var file = new ByteArrayContent(valid);
            file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            validUpload.Add(file, "file", "profile.jpg");
            Assert.Equal(HttpStatusCode.OK,
                (await client.PutAsync("/api/users/me/profile-picture", validUpload)).StatusCode);
        }

        using var invalidUpload = new MultipartFormDataContent();
        var spoofed = new ByteArrayContent("GIF89a".Select(character => (byte)character).ToArray());
        spoofed.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        invalidUpload.Add(spoofed, "file", "profile.gif");
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PutAsync("/api/users/me/profile-picture", invalidUpload)).StatusCode);

        using var image = await client.GetAsync("/api/users/me/profile-picture");
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal(valid, await image.Content.ReadAsByteArrayAsync());
    }

    public void Dispose()
    {
        signingKey.Dispose();
        File.Delete(database);
        if (Directory.Exists(pictureStorage))
        {
            Directory.Delete(pictureStorage, recursive: true);
        }
    }
}
