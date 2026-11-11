using System.Net;
using System.Text;
using GroceryList.Api.Clients;

namespace GroceryList.ApiTests;

public sealed class HttpIngredientClientTests
{
    [Fact]
    public async Task SuccessfulResponseMapsKnownFieldsIgnoresAdditionalFieldsAndUsesExpectedPath()
    {
        using var handler = new ResponseHandler(HttpStatusCode.OK,
            """{"id":7,"name":"Milk","foodGroup":"Dairy","calories":61,"expiryDate":"2028-01-01","extra":"ignored"}""");
        using var client = CreateClient(handler);

        var ingredient = await new HttpIngredientClient(client).GetByIdAsync(7);

        Assert.Equal(new IngredientLookupResult(7, "Milk", "Dairy", 61m), ingredient);
        Assert.Equal(HttpMethod.Get, handler.Request!.Method);
        Assert.Equal("/api/Ingredients/7", handler.Request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task NotFoundResponseIndicatesMissingIngredient()
    {
        using var client = CreateClient(new ResponseHandler(HttpStatusCode.NotFound));

        var exception = await Assert.ThrowsAsync<IngredientNotFoundException>(
            () => new HttpIngredientClient(client).GetByIdAsync(17));

        Assert.Equal(17, exception.IngredientId);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task UpstreamAuthorizationFailuresArePreserved(HttpStatusCode statusCode)
    {
        using var client = CreateClient(new ResponseHandler(statusCode));

        var exception = await Assert.ThrowsAsync<IngredientUpstreamAuthorizationException>(
            () => new HttpIngredientClient(client).GetByIdAsync(7));

        Assert.Equal((int)statusCode, exception.StatusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task UpstreamServerFailuresAreUnavailable(HttpStatusCode statusCode)
    {
        using var client = CreateClient(new ResponseHandler(statusCode));

        await Assert.ThrowsAsync<IngredientLookupUnavailableException>(
            () => new HttpIngredientClient(client).GetByIdAsync(7));
    }

    [Fact]
    public async Task UnexpectedClientErrorIsNotReportedAsDependencyFailure()
    {
        using var client = CreateClient(new ResponseHandler(HttpStatusCode.BadRequest));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new HttpIngredientClient(client).GetByIdAsync(7));
    }

    [Fact]
    public async Task NetworkFailureIsUnavailable()
    {
        using var client = CreateClient(new ThrowingHandler(new HttpRequestException("connection refused")));

        await Assert.ThrowsAsync<IngredientLookupUnavailableException>(
            () => new HttpIngredientClient(client).GetByIdAsync(7));
    }

    [Fact]
    public async Task TimeoutIsUnavailable()
    {
        using var client = CreateClient(new ThrowingHandler(new TaskCanceledException("request timed out")));

        await Assert.ThrowsAsync<IngredientLookupUnavailableException>(
            () => new HttpIngredientClient(client).GetByIdAsync(7));
    }

    [Fact]
    public async Task CallerCancellationPropagates()
    {
        using var cancellation = new CancellationTokenSource();
        using var handler = new BlockingHandler();
        using var client = CreateClient(handler);
        var lookup = new HttpIngredientClient(client).GetByIdAsync(7, cancellation.Token);
        await handler.RequestStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => lookup);
    }

    private static HttpClient CreateClient(HttpMessageHandler handler) => new(handler)
    {
        BaseAddress = new Uri("http://ingredient.test/")
    };

    private sealed class ResponseHandler(HttpStatusCode statusCode, string? content = null) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = content is null ? null : new StringContent(content, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(exception);
    }

    private sealed class BlockingHandler : HttpMessageHandler
    {
        public TaskCompletionSource RequestStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
