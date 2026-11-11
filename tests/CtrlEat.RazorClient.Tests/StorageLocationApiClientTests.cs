using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using CtrlEat.RazorClient.Services;
using CtrlEat.RazorClient.Models;

namespace CtrlEat.RazorClient.Tests;

public sealed class StorageLocationApiClientTests
{
    [Fact]
    public async Task SearchUsesExpectedRouteAndDeserializesResponse()
    {
        var handler = new RecordingHandler("[{\"id\":4,\"name\":\"Freezer\",\"type\":\"Cold\",\"minimumTemperature\":-18,\"maximumTemperature\":-10,\"ingredients\":[],\"createdAt\":\"2026-01-01T00:00:00Z\"}]");
        var client = Create(handler);

        var result = await client.SearchAsync("Freezer", "Cold", CancellationToken.None);

        Assert.Equal("http://storage.test/api/StorageLocations?name=Freezer&type=Cold", handler.Request.RequestUri!.ToString());
        Assert.Equal(4, Assert.Single(result).Id);
        Assert.Equal("Bearer test-token", handler.Request.Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task UnauthorizedResponseProducesActionableApiException()
    {
        var client = Create(new RecordingHandler(string.Empty, HttpStatusCode.Unauthorized));

        var exception = await Assert.ThrowsAsync<ApiException>(() => client.GetAsync(4, CancellationToken.None));

        Assert.Equal(401, exception.StatusCode);
        Assert.Contains("no longer authorized", exception.Message);
    }

    [Fact]
    public async Task CreateUpdateAndDeleteUseExpectedRoutesAndMethods()
    {
        var handler = new RecordingHandler("{\"id\":4,\"name\":\"Freezer\",\"type\":\"Cold\",\"minimumTemperature\":-18,\"maximumTemperature\":-10,\"ingredients\":[],\"createdAt\":\"2026-01-01T00:00:00Z\"}");
        var client = Create(handler);
        var request = new StorageLocationRequest { Name = "Freezer", Type = "Cold", MinimumTemperature = -18, MaximumTemperature = -10, IngredientIds = [7] };

        await client.CreateAsync(request, CancellationToken.None);
        Assert.Equal(HttpMethod.Post, handler.Request.Method);
        Assert.Equal("http://storage.test/api/StorageLocations", handler.Request.RequestUri!.ToString());
        Assert.Contains("\"ingredientIds\":[7]", await handler.Request.Content!.ReadAsStringAsync());

        await client.UpdateAsync(4, request, CancellationToken.None);
        Assert.Equal(HttpMethod.Put, handler.Request.Method);
        Assert.Equal("http://storage.test/api/StorageLocations/4", handler.Request.RequestUri!.ToString());

        await client.DeleteAsync(4, CancellationToken.None);
        Assert.Equal(HttpMethod.Delete, handler.Request.Method);
        Assert.Equal("http://storage.test/api/StorageLocations/4", handler.Request.RequestUri!.ToString());
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ExpectedErrorStatusesProduceReadableMessages(HttpStatusCode status)
    {
        var client = Create(new RecordingHandler("{\"error\":\"Validation failed\"}", status));

        var exception = await Assert.ThrowsAsync<ApiException>(() => client.GetAsync(4, CancellationToken.None));

        Assert.Equal((int)status, exception.StatusCode);
        Assert.NotEmpty(exception.Message);
    }

    [Fact]
    public async Task JsonErrorBodyIsExtractedForValidationResponses()
    {
        var client = Create(new RecordingHandler("{\"error\":\"Name is required.\"}", HttpStatusCode.BadRequest));

        var exception = await Assert.ThrowsAsync<ApiException>(() => client.GetAsync(4, CancellationToken.None));

        Assert.Equal("Name is required.", exception.Message);
    }

    [Fact]
    public async Task ConnectionFailuresBecomeUnavailableErrors()
    {
        var client = Create(new ThrowingHandler(new HttpRequestException("connection refused")));

        var exception = await Assert.ThrowsAsync<ApiException>(() => client.GetAsync(4, CancellationToken.None));

        Assert.Equal(503, exception.StatusCode);
    }

    [Fact]
    public async Task TimeoutsBecomeUnavailableErrors()
    {
        var client = Create(new ThrowingHandler(new TaskCanceledException("timed out")));

        var exception = await Assert.ThrowsAsync<ApiException>(() => client.GetAsync(4, CancellationToken.None));

        Assert.Equal(503, exception.StatusCode);
    }

    private static StorageLocationApiClient Create(HttpMessageHandler handler)
    {
        var context = new DefaultHttpContext();
        context.Features.Set<IAuthenticateResultFeature>(new AuthenticateResultFeature(
            AuthenticateResult.Success(new AuthenticationTicket(new System.Security.Claims.ClaimsPrincipal(), "Cookies"))));
        context.RequestServices = new TokenServiceProvider();
        return new StorageLocationApiClient(new Factory(new HttpClient(handler) { BaseAddress = new Uri("http://storage.test/") }), new HttpContextAccessor { HttpContext = context });
    }

    private sealed class Factory(HttpClient client) : IHttpClientFactory { public HttpClient CreateClient(string name) => client; }
    private sealed class TokenServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType == typeof(IAuthenticationService) ? new TokenAuthenticationService() : null;
    }
    private sealed class TokenAuthenticationService : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) => Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new System.Security.Claims.ClaimsPrincipal(), new AuthenticationProperties(new Dictionary<string, string?> { [".Token.access_token"] = "test-token" }), "Cookies")));
        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignInAsync(HttpContext context, string? scheme, System.Security.Claims.ClaimsPrincipal principal, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
    }
    private sealed class AuthenticateResultFeature(AuthenticateResult result) : IAuthenticateResultFeature { public AuthenticateResult? AuthenticateResult { get; set; } = result; }
    private sealed class RecordingHandler(string json, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public HttpRequestMessage Request { get; private set; } = null!;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        { Request = request; return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json) }); }
    }

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromException<HttpResponseMessage>(exception);
    }
}