using System.Net;
using System.Text;
using StorageLocation.Api.Clients;

namespace StorageLocation.ApiTests;

public sealed class HttpIngredientClientTests
{
    [Fact]
    public async Task SuccessfulResponseDeserializesIngredientAndIgnoresAdditionalFields()
    {
        using var client = CreateClient(HttpStatusCode.OK,
            """{"id":7,"name":"Milk","foodGroup":"Dairy","calories":61,"expiryDate":"2028-01-01","extra":"ignored"}""");
        var ingredient = await new HttpIngredientClient(client).GetByIdAsync(7);

        Assert.Equal(new IngredientLookupResult(7, "Milk", "Dairy", 61), ingredient);
    }

    [Fact]
    public async Task NotFoundResponseIndicatesConfirmedMissingIngredient()
    {
        using var client = CreateClient(HttpStatusCode.NotFound);
        var exception = await Assert.ThrowsAsync<IngredientNotFoundException>(
            () => new HttpIngredientClient(client).GetByIdAsync(17));

        Assert.Equal(17, exception.IngredientId);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task UnauthorizedAndForbiddenResponsesRemainUpstreamAuthorizationFailures(HttpStatusCode statusCode)
    {
        using var client = CreateClient(statusCode);
        var exception = await Assert.ThrowsAsync<IngredientUpstreamAuthorizationException>(
            () => new HttpIngredientClient(client).GetByIdAsync(7));

        Assert.Equal((int)statusCode, exception.StatusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ServerFailuresAreDependencyUnavailable(HttpStatusCode statusCode)
    {
        using var client = CreateClient(statusCode);

        await Assert.ThrowsAsync<IngredientLookupUnavailableException>(
            () => new HttpIngredientClient(client).GetByIdAsync(7));
    }

    [Fact]
    public async Task UnexpectedClientErrorStatusIsNotReportedAsDependencyUnavailable()
    {
        using var client = CreateClient(HttpStatusCode.BadRequest);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new HttpIngredientClient(client).GetByIdAsync(7));
    }

    [Fact]
    public async Task NetworkFailuresAreDependencyUnavailable()
    {
        using var client = new HttpClient(new ThrowingHandler(new HttpRequestException("connection refused")))
        {
            BaseAddress = new Uri("http://ingredient.test/")
        };

        await Assert.ThrowsAsync<IngredientLookupUnavailableException>(
            () => new HttpIngredientClient(client).GetByIdAsync(7));
    }

    [Fact]
    public async Task TimeoutsAreDependencyUnavailable()
    {
        using var client = new HttpClient(new ThrowingHandler(new TaskCanceledException("request timed out")))
        {
            BaseAddress = new Uri("http://ingredient.test/")
        };

        await Assert.ThrowsAsync<IngredientLookupUnavailableException>(
            () => new HttpIngredientClient(client).GetByIdAsync(7));
    }

    [Fact]
    public async Task CallerCancellationIsPropagated()
    {
        using var cancellation = new CancellationTokenSource();
        using var handler = new BlockingHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://ingredient.test/") };
        var lookup = new HttpIngredientClient(client).GetByIdAsync(7, cancellation.Token);
        await handler.RequestStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => lookup);
    }

    private static HttpClient CreateClient(HttpStatusCode statusCode, string? content = null) =>
        new(new ResponseHandler(statusCode, content)) { BaseAddress = new Uri("http://ingredient.test/") };

    private sealed class ResponseHandler(HttpStatusCode statusCode, string? content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.StartsWith("/api/Ingredients/", request.RequestUri!.AbsolutePath);
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