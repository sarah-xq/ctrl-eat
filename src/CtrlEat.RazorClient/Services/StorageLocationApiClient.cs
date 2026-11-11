using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CtrlEat.RazorClient.Models;
using Microsoft.AspNetCore.Authentication;

namespace CtrlEat.RazorClient.Services;

public sealed class StorageLocationApiClient(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor)
{
    public Task<IReadOnlyList<StorageLocationResponse>> SearchAsync(string? name, string? type, CancellationToken cancellationToken) =>
        SendAsync<IReadOnlyList<StorageLocationResponse>>(HttpMethod.Get,
            $"api/StorageLocations?name={Uri.EscapeDataString(name ?? string.Empty)}&type={Uri.EscapeDataString(type ?? string.Empty)}",
            null, cancellationToken);

    public Task<StorageLocationResponse> GetAsync(int id, CancellationToken cancellationToken) =>
        SendAsync<StorageLocationResponse>(HttpMethod.Get, $"api/StorageLocations/{id}", null, cancellationToken);

    public Task<StorageLocationResponse> CreateAsync(StorageLocationRequest request, CancellationToken cancellationToken) =>
        SendAsync<StorageLocationResponse>(HttpMethod.Post, "api/StorageLocations", request, cancellationToken);

    public Task<StorageLocationResponse> UpdateAsync(int id, StorageLocationRequest request, CancellationToken cancellationToken) =>
        SendAsync<StorageLocationResponse>(HttpMethod.Put, $"api/StorageLocations/{id}", request, cancellationToken);

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Delete, $"api/StorageLocations/{id}", null, cancellationToken);
        if (response.IsSuccessStatusCode) return;
        throw await ApiException.CreateAsync(response);
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string uri, object? body, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(method, uri, body, cancellationToken);
        if (!response.IsSuccessStatusCode) throw await ApiException.CreateAsync(response);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new ApiException(502, "The API returned an empty response.");
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string uri, object? body, CancellationToken cancellationToken)
    {
        var token = httpContextAccessor.HttpContext is { } context ? await context.GetTokenAsync("access_token") : null;
        if (string.IsNullOrWhiteSpace(token)) throw new ApiException(401, "Your session has expired. Sign in again.");
        var request = new HttpRequestMessage(method, uri) { Content = body is null ? null : JsonContent.Create(body) };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        try { return await httpClientFactory.CreateClient("StorageLocationApi").SendAsync(request, cancellationToken); }
        catch (HttpRequestException exception) { throw new ApiException(503, "The Storage Location API is unavailable.", exception); }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested) { throw new ApiException(503, "The Storage Location API timed out.", exception); }
    }
}

public sealed class ApiException(int statusCode, string message, Exception? inner = null) : Exception(message, inner)
{
    public int StatusCode { get; } = statusCode;

    public static async Task<ApiException> CreateAsync(HttpResponseMessage response)
    {
        var detail = await response.Content.ReadAsStringAsync();
        var message = TryGetErrorMessage(detail);
        return new ApiException((int)response.StatusCode, response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Your session is no longer authorized. Sign in again.",
            HttpStatusCode.Forbidden => "Your account is not allowed to perform this action.",
            HttpStatusCode.NotFound => "The storage location was not found.",
            HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable => "The ingredient dependency or Storage Location API is unavailable.",
            _ => message ?? $"The API returned HTTP {(int)response.StatusCode}."
        });
    }

    private static string? TryGetErrorMessage(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody)) return null;
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("error", out var error)
                && error.ValueKind == JsonValueKind.String)
            {
                return error.GetString();
            }
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("title", out var title)
                && title.ValueKind == JsonValueKind.String)
            {
                return title.GetString();
            }
        }
        catch (JsonException) { }
        return null;
    }
}