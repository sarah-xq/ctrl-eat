using System.Net;
using System.Net.Http.Json;
using System.Text;
using CtrlEat.RazorClient.Models;
using CtrlEat.RazorClient.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CtrlEat.RazorClient.Tests;

public sealed class GroceryListApiClientTests
{
    [Fact]
    public async Task GetAllFiltersDateAndForwardsAccessToken()
    {
        var handler = new RecordingHandler("[{\"id\":4,\"date\":\"2026-03-04T00:00:00\",\"createdAt\":\"2026-03-01T10:00:00Z\",\"ingredients\":[{\"id\":2,\"name\":\"Apple\",\"foodGroup\":\"Fruit\",\"calories\":95}]}]");
        var client = Create(handler);

        var result = await client.GetAllAsync(new DateTime(2026, 3, 4, 18, 30, 0), CancellationToken.None);

        Assert.Equal("http://grocery.test/api/GroceryLists?date=2026-03-04", handler.Request.RequestUri!.ToString());
        Assert.Equal("Bearer test-token", handler.Request.Headers.Authorization!.ToString());
        var list = Assert.Single(result);
        Assert.Equal(4, list.Id);
        var ingredient = Assert.Single(list.Ingredients);
        Assert.Equal("Apple", ingredient.Name);
        Assert.Equal(95m, ingredient.Calories);
    }

    [Fact]
    public async Task CreateUpdateAndDeleteUseExpectedMethodsRoutesAndPayload()
    {
        var handler = new RecordingHandler("{\"id\":4,\"date\":\"2026-03-04T00:00:00\",\"createdAt\":\"2026-03-01T10:00:00Z\",\"ingredients\":[]}");
        var client = Create(handler);
        var request = new GroceryListRequest { Date = new DateTime(2026, 3, 4, 21, 15, 0), IngredientIds = [2, 2] };

        await client.CreateAsync(request, CancellationToken.None);
        Assert.Equal(HttpMethod.Post, handler.Request.Method);
        Assert.Equal("http://grocery.test/api/GroceryLists", handler.Request.RequestUri!.ToString());
        Assert.Contains("\"date\":\"2026-03-04T00:00:00", handler.RequestBody);
        Assert.Contains("\"ingredientIds\":[2]", handler.RequestBody);

        await client.UpdateAsync(4, request, CancellationToken.None);
        Assert.Equal(HttpMethod.Put, handler.Request.Method);
        Assert.Equal("http://grocery.test/api/GroceryLists/4", handler.Request.RequestUri!.ToString());

        await client.DeleteAsync(4, CancellationToken.None);
        Assert.Equal(HttpMethod.Delete, handler.Request.Method);
        Assert.Equal("http://grocery.test/api/GroceryLists/4", handler.Request.RequestUri!.ToString());
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ExpectedApiErrorsHaveStatusAndHelpfulMessage(HttpStatusCode status)
    {
        var client = Create(new RecordingHandler("{\"title\":\"Request rejected\"}", status));

        var exception = await Assert.ThrowsAsync<GroceryListApiException>(
            () => client.GetAsync(4, CancellationToken.None));

        Assert.Equal((int)status, exception.StatusCode);
        Assert.NotEmpty(exception.Message);
    }

    [Fact]
    public async Task MissingAccessTokenReturnsUnauthorizedWithoutSendingRequest()
    {
        var factory = new TestFactory(new HttpClient(new RecordingHandler("{}")) { BaseAddress = new Uri("http://grocery.test/") });
        var context = new DefaultHttpContext();
        context.RequestServices = new TokenServiceProvider(includeToken: false);
        var client = new GroceryListApiClient(factory, new HttpContextAccessor { HttpContext = context });

        var exception = await Assert.ThrowsAsync<GroceryListApiException>(
            () => client.GetAllAsync(null, CancellationToken.None));

        Assert.Equal(401, exception.StatusCode);
        Assert.Equal(0, factory.CallCount);
    }

    [Fact]
    public async Task NetworkFailureMapsToServiceUnavailable()
    {
        var client = Create(new ThrowingHandler(new HttpRequestException("connection refused")));

        var exception = await Assert.ThrowsAsync<GroceryListApiException>(
            () => client.GetAllAsync(null, CancellationToken.None));

        Assert.Equal(503, exception.StatusCode);
    }

    private static GroceryListApiClient Create(HttpMessageHandler handler)
    {
        var context = new DefaultHttpContext();
        context.Features.Set<IAuthenticateResultFeature>(new AuthenticateResultFeature(
            AuthenticateResult.Success(new AuthenticationTicket(new System.Security.Claims.ClaimsPrincipal(), "Cookies"))));
        context.RequestServices = new TokenServiceProvider(includeToken: true);
        return new GroceryListApiClient(
            new TestFactory(new HttpClient(handler) { BaseAddress = new Uri("http://grocery.test/") }),
            new HttpContextAccessor { HttpContext = context });
    }

    private sealed class TestFactory(HttpClient client) : IHttpClientFactory
    {
        public int CallCount { get; private set; }
        public HttpClient CreateClient(string name) { CallCount++; Assert.Equal("GroceryListApi", name); return client; }
    }

    private sealed class TokenServiceProvider : IServiceProvider
    {
        private readonly bool includeToken;
        public TokenServiceProvider(bool includeToken) => this.includeToken = includeToken;
        public object? GetService(Type serviceType) => serviceType == typeof(IAuthenticationService) ? new TokenAuthenticationService(includeToken) : null;
    }

    private sealed class TokenAuthenticationService(bool includeToken) : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
        {
            var properties = includeToken
                ? new AuthenticationProperties(new Dictionary<string, string?> { [".Token.access_token"] = "test-token" })
                : new AuthenticationProperties();
            return
            Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
                new System.Security.Claims.ClaimsPrincipal(),
                properties,
                "Cookies")));
        }
        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignInAsync(HttpContext context, string? scheme, System.Security.Claims.ClaimsPrincipal principal, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
    }

    private sealed class AuthenticateResultFeature(AuthenticateResult result) : IAuthenticateResultFeature
    {
        public AuthenticateResult? AuthenticateResult { get; set; } = result;
    }

    private sealed class RecordingHandler(string json, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public HttpRequestMessage Request { get; private set; } = null!;
        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            RequestBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(exception);
    }
}
