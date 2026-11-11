using System.Net;
using System.Net.Http.Json;

namespace StorageLocation.Api.Clients;

public sealed class DevelopmentIngredientHandler : HttpMessageHandler
{
    private static readonly IReadOnlyDictionary<int, IngredientLookupResult> Ingredients =
        new Dictionary<int, IngredientLookupResult>
        {
            [7] = new(7, "Milk", "Dairy", 61),
            [8] = new(8, "Apples", "Fruit", 52)
        };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.Method != HttpMethod.Get
            || request.RequestUri?.AbsolutePath is not { } path
            || !path.StartsWith("/api/Ingredients/", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        var idText = request.RequestUri?.Segments.LastOrDefault()?.Trim('/');
        if (!int.TryParse(idText, out var id))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
        }

        if (id == 401)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        }

        if (id == 403)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden));
        }

        if (id == 500)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }

        if (id == 599)
        {
            throw new HttpRequestException("Simulated Ingredient API connection failure.");
        }

        if (id == 598)
        {
            throw new TaskCanceledException("Simulated Ingredient API timeout.");
        }

        if (!Ingredients.TryGetValue(id, out var ingredient))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                ingredient.Id,
                ingredient.Name,
                ingredient.FoodGroup,
                ingredient.Calories,
                simulatedField = "ignored"
            })
        });
    }
}