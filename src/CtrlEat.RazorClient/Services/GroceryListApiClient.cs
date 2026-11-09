using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CtrlEat.RazorClient.Models;
using Microsoft.AspNetCore.Authentication;

namespace CtrlEat.RazorClient.Services;

public sealed class GroceryListApiClient(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor)
{
    public Task<IReadOnlyList<GroceryListResponse>> GetAllAsync(DateTime? date, CancellationToken cancellationToken)
    {
        var uri = date.HasValue
            ? $"api/GroceryLists?date={Uri.EscapeDataString(date.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}"
            : "api/GroceryLists";
        return SendAsync<IReadOnlyList<GroceryListResponse>>(HttpMethod.Get, uri, null, cancellationToken);
    }

    public Task<GroceryListResponse> GetAsync(int id, CancellationToken cancellationToken) =>
        SendAsync<GroceryListResponse>(HttpMethod.Get, $"api/GroceryLists/{id}", null, cancellationToken);

    public Task<GroceryListResponse> CreateAsync(GroceryListRequest request, CancellationToken cancellationToken) =>
        SendAsync<GroceryListResponse>(HttpMethod.Post, "api/GroceryLists", Normalize(request), cancellationToken);

    public Task<GroceryListResponse> UpdateAsync(int id, GroceryListRequest request, CancellationToken cancellationToken) =>
        SendAsync<GroceryListResponse>(HttpMethod.Put, $"api/GroceryLists/{id}", Normalize(request), cancellationToken);

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Delete, $"api/GroceryLists/{id}", null, cancellationToken);
        if (response.IsSuccessStatusCode) return;
        throw await GroceryListApiException.CreateAsync(response);
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string uri, object? body, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(method, uri, body, cancellationToken);
        if (!response.IsSuccessStatusCode) throw await GroceryListApiException.CreateAsync(response);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new GroceryListApiException(502, "The Grocery List API returned an empty response.");
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string uri, object? body, CancellationToken cancellationToken)
    {
        var token = httpContextAccessor.HttpContext is { } context ? await context.GetTokenAsync("access_token") : null;
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new GroceryListApiException(401, "Your session has expired. Sign in again.");
        }

        using var request = new HttpRequestMessage(method, uri)
        {
            Content = body is null ? null : JsonContent.Create(body)
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        try
        {
            return await httpClientFactory.CreateClient("GroceryListApi").SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new GroceryListApiException(503, "The Grocery List API is unavailable.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new GroceryListApiException(503, "The Grocery List API timed out.", exception);
        }
    }

    private static GroceryListRequest Normalize(GroceryListRequest request) => new()
    {
        Date = request.Date.Date,
        IngredientIds = request.IngredientIds?.Distinct().ToList() ?? []
    };
}

public sealed class GroceryListApiException(int statusCode, string message, Exception? inner = null) : Exception(message, inner)
{
    public int StatusCode { get; } = statusCode;

    public static async Task<GroceryListApiException> CreateAsync(HttpResponseMessage response)
    {
        var detail = await response.Content.ReadAsStringAsync();
        var message = TryGetErrorMessage(detail);
        return new GroceryListApiException((int)response.StatusCode, response.StatusCode switch
        {
            HttpStatusCode.BadRequest => message ?? "The Grocery List request is invalid.",
            HttpStatusCode.Unauthorized => "Your session is no longer authorized. Sign in again.",
            HttpStatusCode.Forbidden => "Your account is not allowed to perform this action.",
            HttpStatusCode.NotFound => "The Grocery List was not found.",
            HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable => "The Ingredient dependency or Grocery List API is unavailable.",
            _ => message ?? $"The API returned HTTP {(int)response.StatusCode}."
        });
    }

    private static string? TryGetErrorMessage(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody)) return null;
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            foreach (var name in new[] { "error", "title", "detail" })
            {
                if (document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty(name, out var value)
                    && value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString();
                }
            }
        }
        catch (JsonException) { }
        return null;
    }
}
