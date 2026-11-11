using System.Net;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using StorageLocation.Api.Clients;
using StorageLocation.Api.DTOs;

namespace StorageLocation.ApiTests;

public sealed class StorageLocationsApiTests
{
    private const string TestIssuer = "https://storage-issuer.test";
    private const string TestAudience = "storage-api-tests";

    [Fact]
    public async Task CreateGetUpdateAndDeleteUseTheAgreedRoutesAndResponses()
    {
        using var host = CreateHost();
        var client = host.Client;

        using var createResponse = await client.PostAsJsonAsync("/api/StorageLocations", Request(
            name: " Main fridge ", type: " Cold ", ingredientIds: [7, 7, 8]));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await ReadResponseAsync(createResponse);
        Assert.True(created.Id > 0);
        Assert.Equal("Main fridge", created.Name);
        Assert.Equal("Cold", created.Type);
        Assert.Equal(new[] { 7, 8 }, created.Ingredients.Select(ingredient => ingredient.Id));
        Assert.Equal("Milk", created.Ingredients[0].Name);

        using var getResponse = await client.GetAsync($"/api/StorageLocations/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await ReadResponseAsync(getResponse);
        Assert.Equal(created.Id, fetched.Id);

        using var updateResponse = await client.PutAsJsonAsync(
            $"/api/StorageLocations/{created.Id}",
            Request("Updated fridge", "Chilled", 2, 6, [8]));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await ReadResponseAsync(updateResponse);
        Assert.Equal("Updated fridge", updated.Name);
        Assert.Equal("Chilled", updated.Type);
        Assert.Equal(2, updated.MinimumTemperature);
        Assert.Equal(6, updated.MaximumTemperature);
        Assert.Equal(new[] { 8 }, updated.Ingredients.Select(ingredient => ingredient.Id));

        using var deleteResponse = await client.DeleteAsync($"/api/StorageLocations/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/StorageLocations/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task GetStorageLocationsFiltersByNameAndType()
    {
        using var host = CreateHost();
        var client = host.Client;
        (await client.PostAsJsonAsync("/api/StorageLocations", Request("Main fridge", "Cold"))).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/StorageLocations", Request("Chest freezer", "Frozen"))).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/StorageLocations", Request("Pantry", "Dry"))).EnsureSuccessStatusCode();

        using var response = await client.GetAsync("/api/StorageLocations?name=freezer&type=Frozen");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<List<StorageLocationResponse>>();

        Assert.Equal("Chest freezer", Assert.Single(results!).Name);
    }

    [Fact]
    public async Task InvalidDomainValuesAndUnknownIngredientsReturnBadRequestWithoutPersistence()
    {
        using var host = CreateHost();
        var client = host.Client;

        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/StorageLocations", Request(" ", "Cold"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/StorageLocations", Request("Pantry", "Dry", 5, 4))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/StorageLocations", Request("Pantry", "Dry", ingredientIds: [999]))).StatusCode);

        using var listResponse = await client.GetAsync("/api/StorageLocations");
        var results = await listResponse.Content.ReadFromJsonAsync<List<StorageLocationResponse>>();
        Assert.Empty(results!);
    }

    [Fact]
    public async Task MissingLocationsReturnNotFoundForGetUpdateAndDelete()
    {
        using var host = CreateHost();
        var client = host.Client;
        const int missingId = 999;

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/StorageLocations/{missingId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PutAsJsonAsync($"/api/StorageLocations/{missingId}", Request("Pantry", "Dry"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/StorageLocations/{missingId}")).StatusCode);
    }

    [Fact]
    public async Task APIStartsWithoutIngredientClientAndFailsClosedWhenIngredientIdsAreSubmitted()
    {
        using var host = CreateHost(registerIngredientClient: false);
        var client = host.Client;

        using var emptyCreate = await client.PostAsJsonAsync("/api/StorageLocations", Request("Pantry", "Dry"));
        Assert.Equal(HttpStatusCode.Created, emptyCreate.StatusCode);

        using var unresolvedCreate = await client.PostAsJsonAsync(
            "/api/StorageLocations", Request("Cold box", "Cold", ingredientIds: [7]));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unresolvedCreate.StatusCode);

        using var listResponse = await client.GetAsync("/api/StorageLocations");
        var results = await listResponse.Content.ReadFromJsonAsync<List<StorageLocationResponse>>();
        Assert.Single(results!);
    }

    [Fact]
    public async Task ConnectionFailureReturnsServiceUnavailableWithoutCreatingOrUpdatingLocations()
    {
        await AssertIngredientDependencyFailureDoesNotPersistAsync(new ConnectionFailureIngredientClient());
    }

    [Fact]
    public async Task IngredientTimeoutReturnsServiceUnavailableWithoutCreatingOrUpdatingLocations()
    {
        await AssertIngredientDependencyFailureDoesNotPersistAsync(new TimeoutIngredientClient());
    }

    [Fact]
    public async Task UnexpectedIngredientClientFailureIsNotReportedAsDependencyUnavailable()
    {
        using var host = CreateHost(new UnexpectedFailureIngredientClient());
        using var response = await host.Client.PostAsJsonAsync(
            "/api/StorageLocations", Request("Failure", "Cold", ingredientIds: [7]));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task CallerCancellationIsNotReportedAsIngredientTimeout()
    {
        var ingredientClient = new CancellationIngredientClient();
        using var host = CreateHost(ingredientClient);
        using var cancellation = new CancellationTokenSource();
        var request = host.Client.PostAsJsonAsync(
            "/api/StorageLocations",
            Request("Cancelled", "Cold", ingredientIds: [7]),
            cancellation.Token);

        await ingredientClient.LookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);

        using var listResponse = await host.Client.GetAsync("/api/StorageLocations");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var locations = await listResponse.Content.ReadFromJsonAsync<List<StorageLocationResponse>>();
        Assert.Empty(locations!);
    }

    [Fact]
    public async Task ProtectedRoutesRejectMissingInvalidExpiredAndWrongAudienceTokensButHealthIsPublic()
    {
        using var key = RSA.Create(2048);
        using var host = CreateHost(signingKey: key);
        using var client = host.Client;

        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/StorageLocations")).StatusCode);

        foreach (var token in new[]
        {
            "not-a-jwt",
            CreateToken(key, scope: "storage-locations.read", expired: true),
            CreateToken(key, scope: "storage-locations.read", audience: "wrong-audience")
        })
        {
            SetToken(client, token);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/StorageLocations")).StatusCode);
        }
    }

    [Fact]
    public async Task ReadAndWritePoliciesRequireTheirOwnScopes()
    {
        using var key = RSA.Create(2048);
        using var host = CreateHost(signingKey: key);
        using var client = host.Client;

        SetToken(client, CreateToken(key, scope: "storage-locations.read"));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/StorageLocations")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PostAsJsonAsync("/api/StorageLocations", Request("Pantry", "Dry"))).StatusCode);

        SetToken(client, CreateToken(key, scope: "storage-locations.write"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/StorageLocations")).StatusCode);
        using var created = await client.PostAsJsonAsync("/api/StorageLocations", Request("Pantry", "Dry"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var storageLocation = await ReadResponseAsync(created);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(
            $"/api/StorageLocations/{storageLocation.Id}", Request("Updated pantry", "Dry"))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(
            $"/api/StorageLocations/{storageLocation.Id}")).StatusCode);
    }

    [Fact]
    public async Task DevelopmentHttpTransportProvidesIngredientDetailsForCreateAndRead()
    {
        using var host = CreateHost(environment: "Development", useConfiguredIngredientClient: true);

        using var createResponse = await host.Client.PostAsJsonAsync(
            "/api/StorageLocations", Request("Development fridge", "Cold", ingredientIds: [7]));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await ReadResponseAsync(createResponse);
        var createdIngredient = Assert.Single(created.Ingredients);
        Assert.Equal(7, createdIngredient.Id);
        Assert.Equal("Milk", createdIngredient.Name);
        Assert.Equal("Dairy", createdIngredient.FoodGroup);
        Assert.Equal(61, createdIngredient.Calories);

        using var getResponse = await host.Client.GetAsync($"/api/StorageLocations/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await ReadResponseAsync(getResponse);
        Assert.Equal(createdIngredient, Assert.Single(fetched.Ingredients));
    }

    [Fact]
    public async Task ProductionEnvironmentDoesNotRegisterDevelopmentIngredientTransport()
    {
        using var host = CreateHost(environment: "Production", useConfiguredIngredientClient: true);
        using var response = await host.Client.PostAsJsonAsync(
            "/api/StorageLocations", Request("Production fridge", "Cold", ingredientIds: [7]));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task DevelopmentTransportSimulatesUpstreamErrorsAndMissingIngredients()
    {
        using var host = CreateHost(environment: "Development", useConfiguredIngredientClient: true);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await host.Client.PostAsJsonAsync(
                "/api/StorageLocations", Request("Missing ingredient", "Cold", ingredientIds: [999]))).StatusCode);
        Assert.Equal(HttpStatusCode.BadGateway,
            (await host.Client.PostAsJsonAsync(
                "/api/StorageLocations", Request("Unauthorized upstream", "Cold", ingredientIds: [401]))).StatusCode);
        Assert.Equal(HttpStatusCode.BadGateway,
            (await host.Client.PostAsJsonAsync(
                "/api/StorageLocations", Request("Forbidden upstream", "Cold", ingredientIds: [403]))).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable,
            (await host.Client.PostAsJsonAsync(
                "/api/StorageLocations", Request("Unavailable upstream", "Cold", ingredientIds: [500]))).StatusCode);

        using var listResponse = await host.Client.GetAsync("/api/StorageLocations");
        var locations = await listResponse.Content.ReadFromJsonAsync<List<StorageLocationResponse>>();
        Assert.Empty(locations!);
    }

    private static async Task AssertIngredientDependencyFailureDoesNotPersistAsync(IIngredientClient ingredientClient)
    {
        using var host = CreateHost(ingredientClient);
        var client = host.Client;

        using var failedCreate = await client.PostAsJsonAsync(
            "/api/StorageLocations", Request("Failed create", "Cold", ingredientIds: [7]));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failedCreate.StatusCode);

        using var createWithoutIngredients = await client.PostAsJsonAsync(
            "/api/StorageLocations", Request("Existing location", "Dry"));
        Assert.Equal(HttpStatusCode.Created, createWithoutIngredients.StatusCode);
        var existing = await ReadResponseAsync(createWithoutIngredients);

        using var failedUpdate = await client.PutAsJsonAsync(
            $"/api/StorageLocations/{existing.Id}", Request("Changed location", "Cold", ingredientIds: [7]));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failedUpdate.StatusCode);

        using var getResponse = await client.GetAsync($"/api/StorageLocations/{existing.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var persisted = await ReadResponseAsync(getResponse);
        Assert.Equal("Existing location", persisted.Name);
        Assert.Equal("Dry", persisted.Type);
        Assert.Empty(persisted.Ingredients);

        using var listResponse = await client.GetAsync("/api/StorageLocations");
        var locations = await listResponse.Content.ReadFromJsonAsync<List<StorageLocationResponse>>();
        Assert.Equal("Existing location", Assert.Single(locations!).Name);
    }

    private static CreateStorageLocationRequest Request(
        string? name,
        string? type,
        decimal minimumTemperature = 0,
        decimal maximumTemperature = 10,
        List<int>? ingredientIds = null) => new()
    {
        Name = name,
        Type = type,
        MinimumTemperature = minimumTemperature,
        MaximumTemperature = maximumTemperature,
        IngredientIds = ingredientIds ?? []
    };

    private static async Task<StorageLocationResponse> ReadResponseAsync(HttpResponseMessage response)
    {
        var result = await response.Content.ReadFromJsonAsync<StorageLocationResponse>();
        return Assert.IsType<StorageLocationResponse>(result);
    }

    private static TestHost CreateHost(
        IIngredientClient? customIngredientClient = null,
        bool registerIngredientClient = true,
        string environment = "Testing",
        bool useConfiguredIngredientClient = false,
        RSA? signingKey = null)
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"storage-location-api-{Guid.NewGuid():N}.db");
        signingKey ??= RSA.Create(2048);
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("ConnectionStrings:StorageLocation", $"Data Source={databasePath};Pooling=False");
            builder.UseSetting("IngredientApi:BaseUrl", "http://ingredient.test/");
            builder.UseSetting("IngredientApi:UseSimulatedTransport", useConfiguredIngredientClient ? "true" : "false");
            builder.UseSetting("Authentication:Authority", TestIssuer);
            builder.UseSetting("Authentication:Audience", TestAudience);
            builder.ConfigureTestServices(services =>
            {
                if (!registerIngredientClient || !useConfiguredIngredientClient)
                {
                    services.RemoveAll<IIngredientClient>();
                }

                if (registerIngredientClient && !useConfiguredIngredientClient)
                {
                    if (customIngredientClient is null)
                    {
                        services.AddSingleton<IIngredientClient, TestIngredientClient>();
                    }
                    else
                    {
                        services.AddSingleton(customIngredientClient);
                    }
                }

                if (signingKey is not null)
                {
                    services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                    {
                        var discovery = new OpenIdConnectConfiguration { Issuer = TestIssuer };
                        discovery.SigningKeys.Add(new RsaSecurityKey(signingKey) { KeyId = "storage-test" });
                        options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(discovery);
                    });
                }
            });
        });

        return new TestHost(factory, databasePath, signingKey);
    }

    private static void SetToken(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private static string CreateToken(
        RSA signingKey,
        string scope,
        string issuer = TestIssuer,
        string audience = TestAudience,
        bool expired = false)
    {
        var now = DateTime.UtcNow;
        var credentials = new SigningCredentials(new RsaSecurityKey(signingKey) { KeyId = "storage-test" }, SecurityAlgorithms.RsaSha256);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            [new Claim("sub", "storage-test-user"), new Claim("scope", scope)],
            now.AddMinutes(-2),
            expired ? now.AddMinutes(-1) : now.AddMinutes(10),
            credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class TestHost : IDisposable
    {
        private readonly WebApplicationFactory<Program> factory;
        private readonly string databasePath;
        private readonly RSA signingKey;
        public HttpClient Client { get; }

        public TestHost(WebApplicationFactory<Program> factory, string databasePath, RSA signingKey)
        {
            this.factory = factory;
            this.databasePath = databasePath;
            this.signingKey = signingKey;
            Client = factory.CreateClient();
            SetToken(Client, CreateToken(signingKey, "storage-locations.read storage-locations.write"));
        }

        public void Dispose()
        {
            Client.Dispose();
            factory.Dispose();
            signingKey.Dispose();
            SqliteConnection.ClearAllPools();
            foreach (var suffix in new[] { string.Empty, "-shm", "-wal" })
            {
                var path = databasePath + suffix;
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }

    private sealed class TestIngredientClient : IIngredientClient
    {
        private static readonly IReadOnlyDictionary<int, IngredientLookupResult> Ingredients =
            new Dictionary<int, IngredientLookupResult>
            {
                [7] = new(7, "Milk", "Dairy", 61),
                [8] = new(8, "Apples", "Fruit", 52)
            };

        public Task<IngredientLookupResult?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            Ingredients.TryGetValue(id, out var ingredient);
            return Task.FromResult(ingredient);
        }
    }

    private sealed class UnexpectedFailureIngredientClient : IIngredientClient
    {
        public Task<IngredientLookupResult?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromException<IngredientLookupResult?>(new InvalidOperationException("Unexpected client bug."));
    }

    private sealed class CancellationIngredientClient : IIngredientClient
    {
        public TaskCompletionSource LookupStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<IngredientLookupResult?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            LookupStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return null;
        }
    }

    private sealed class ConnectionFailureIngredientClient : IIngredientClient
    {
        public Task<IngredientLookupResult?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromException<IngredientLookupResult?>(
                new HttpRequestException("Simulated connection failure to ingredient service."));
    }

    private sealed class TimeoutIngredientClient : IIngredientClient
    {
        public Task<IngredientLookupResult?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromException<IngredientLookupResult?>(
                new TaskCanceledException("Simulated timeout calling ingredient service."));
    }
}
