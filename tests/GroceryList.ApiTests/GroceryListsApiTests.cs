using System.Net;
using System.Net.Http.Json;
using GroceryList.Api.Clients;
using GroceryList.Api.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using GroceryList.Data;

namespace GroceryList.ApiTests;

public sealed class GroceryListsApiTests
{
    [Fact]
    public async Task CreateGetUpdateAndDeleteUseTheAgreedRoutesAndResponseFields()
    {
        using var host = CreateHost();
        var client = host.Client;

        using var createResponse = await client.PostAsJsonAsync("/api/GroceryLists", Request(
            new DateTime(2026, 11, 12, 14, 30, 0), [1, 2, 1]));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<GroceryListResponse>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal(new DateTime(2026, 11, 12), created.Date);
        Assert.True(created.CreatedAt > DateTime.MinValue);
        Assert.Equal(new[] { 1, 2 }, created.Ingredients.Select(ingredient => ingredient.Id));
        Assert.Equal("Milk", created.Ingredients[0].Name);
        Assert.Equal("Dairy", created.Ingredients[0].FoodGroup);
        Assert.Equal(150m, created.Ingredients[0].Calories);
        Assert.NotNull(createResponse.Headers.Location);

        using var getResponse = await client.GetAsync($"/api/GroceryLists/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<GroceryListResponse>();
        Assert.Equal(created.Id, fetched!.Id);

        using var updateResponse = await client.PutAsJsonAsync(
            $"/api/GroceryLists/{created.Id}", Request(new DateTime(2026, 11, 13, 9, 0, 0), [2]));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<GroceryListResponse>();
        Assert.Equal(new DateTime(2026, 11, 13), updated!.Date);
        Assert.Equal(new[] { 2 }, updated.Ingredients.Select(ingredient => ingredient.Id));

        using var deleteResponse = await client.DeleteAsync($"/api/GroceryLists/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/GroceryLists/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task GetGroceryListsFiltersByDateOnly()
    {
        using var host = CreateHost();
        var client = host.Client;
        await client.PostAsJsonAsync("/api/GroceryLists", Request(new DateTime(2026, 11, 12, 10, 0, 0)));
        await client.PostAsJsonAsync("/api/GroceryLists", Request(new DateTime(2026, 11, 12, 20, 0, 0)));
        await client.PostAsJsonAsync("/api/GroceryLists", Request(new DateTime(2026, 11, 13, 10, 0, 0)));

        using var response = await client.GetAsync("/api/GroceryLists?date=2026-11-12T23:15:00");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<List<GroceryListResponse>>();

        Assert.Equal(2, results!.Count);
        Assert.All(results, result => Assert.Equal(new DateTime(2026, 11, 12), result.Date));
    }

    [Fact]
    public async Task NullAndEmptyIngredientIdsAreAccepted()
    {
        using var host = CreateHost();
        var client = host.Client;

        using var nullResponse = await client.PostAsJsonAsync("/api/GroceryLists", Request(new DateTime(2026, 11, 12), null));
        using var emptyResponse = await client.PostAsJsonAsync("/api/GroceryLists", Request(new DateTime(2026, 11, 13), []));

        Assert.Equal(HttpStatusCode.Created, nullResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, emptyResponse.StatusCode);
        Assert.Empty((await nullResponse.Content.ReadFromJsonAsync<GroceryListResponse>())!.Ingredients);
        Assert.Empty((await emptyResponse.Content.ReadFromJsonAsync<GroceryListResponse>())!.Ingredients);
    }

    [Fact]
    public async Task NonpositiveIngredientIdsReturnBadRequest()
    {
        using var host = CreateHost();
        var client = host.Client;

        using var zeroResponse = await client.PostAsJsonAsync("/api/GroceryLists", Request(new DateTime(2026, 11, 12), [0]));
        using var negativeResponse = await client.PostAsJsonAsync("/api/GroceryLists", Request(new DateTime(2026, 11, 12), [-1]));

        Assert.Equal(HttpStatusCode.BadRequest, zeroResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, negativeResponse.StatusCode);
    }

    [Fact]
    public async Task MissingIngredientsReturnBadRequestWithoutPersistingCreateOrUpdate()
    {
        using var host = CreateHost();
        var client = host.Client;
        using var createResponse = await client.PostAsJsonAsync("/api/GroceryLists", Request(new DateTime(2026, 11, 12)));
        var created = await createResponse.Content.ReadFromJsonAsync<GroceryListResponse>();

        using var invalidCreate = await client.PostAsJsonAsync("/api/GroceryLists", Request(new DateTime(2026, 11, 13), [999]));
        using var invalidUpdate = await client.PutAsJsonAsync($"/api/GroceryLists/{created!.Id}", Request(new DateTime(2026, 11, 14), [999]));
        using var unchangedResponse = await client.GetAsync($"/api/GroceryLists/{created.Id}");
        var unchanged = await unchangedResponse.Content.ReadFromJsonAsync<GroceryListResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, invalidCreate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidUpdate.StatusCode);
        Assert.Equal(new DateTime(2026, 11, 12), unchanged!.Date);
    }

    [Fact]
    public async Task MissingGroceryListIdsReturnNotFoundForGetUpdateAndDelete()
    {
        using var host = CreateHost();
        var client = host.Client;
        const int missingId = 999;

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/GroceryLists/{missingId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PutAsJsonAsync($"/api/GroceryLists/{missingId}", Request(new DateTime(2026, 11, 12)))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/GroceryLists/{missingId}")).StatusCode);
    }

    [Fact]
    public async Task UnavailableIngredientDependencyReturnsServiceUnavailable()
    {
        using var host = CreateHost(new FakeIngredientClient(_ => throw new IngredientLookupUnavailableException()));

        using var response = await host.Client.PostAsJsonAsync(
            "/api/GroceryLists", Request(new DateTime(2026, 11, 12), [1]));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task IngredientTimeoutReturnsServiceUnavailable()
    {
        using var host = CreateHost(new FakeIngredientClient(_ => throw new TimeoutException()));

        using var response = await host.Client.PostAsJsonAsync(
            "/api/GroceryLists", Request(new DateTime(2026, 11, 12), [1]));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task IngredientAuthorizationFailureReturnsBadGateway()
    {
        using var host = CreateHost(new FakeIngredientClient(_ => throw new IngredientUpstreamAuthorizationException(403)));

        using var response = await host.Client.PostAsJsonAsync(
            "/api/GroceryLists", Request(new DateTime(2026, 11, 12), [1]));

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDoesNotCallIngredientClient()
    {
        var ingredientClient = new FakeIngredientClient();
        using var host = CreateHost(ingredientClient);
        using var createResponse = await host.Client.PostAsJsonAsync(
            "/api/GroceryLists", Request(new DateTime(2026, 11, 12), [1]));
        var created = await createResponse.Content.ReadFromJsonAsync<GroceryListResponse>();
        ingredientClient.ResetCallCount();

        using var deleteResponse = await host.Client.DeleteAsync($"/api/GroceryLists/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(0, ingredientClient.CallCount);
    }

    private static object Request(DateTime date, int[]? ingredientIds = null) => new
    {
        IngredientIds = ingredientIds,
        Date = date
    };

    private static TestHost CreateHost(FakeIngredientClient? ingredientClient = null)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<GroceryListDbContext>>();
                services.RemoveAll<GroceryListDbContext>();
                var connection = new SqliteConnection("Data Source=:memory:");
                connection.Open();
                services.AddSingleton(connection);
                services.AddDbContext<GroceryListDbContext>((provider, options) =>
                    options.UseSqlite(provider.GetRequiredService<SqliteConnection>()));
                services.RemoveAll<IIngredientClient>();
                services.AddSingleton<IIngredientClient>(ingredientClient ?? new FakeIngredientClient());
            });
        });
        return new TestHost(factory, ingredientClient);
    }

    private sealed class TestHost(WebApplicationFactory<Program> factory, FakeIngredientClient? ingredientClient) : IDisposable
    {
        public HttpClient Client { get; } = factory.CreateClient();

        public void Dispose()
        {
            Client.Dispose();
            factory.Dispose();
            ingredientClient?.ResetCallCount();
        }
    }

    private sealed class FakeIngredientClient(Func<int, IngredientLookupResult?>? lookup = null) : IIngredientClient
    {
        private int callCount;

        public int CallCount => Volatile.Read(ref callCount);

        public Task<IngredientLookupResult?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref callCount);
            return Task.FromResult(lookup is not null
                ? lookup(id)
                : id switch
                {
                    1 => new IngredientLookupResult(1, "Milk", "Dairy", 150m),
                    2 => new IngredientLookupResult(2, "Apple", "Fruit", 95m),
                    _ => null
                });
        }

        public void ResetCallCount() => Interlocked.Exchange(ref callCount, 0);
    }
}
