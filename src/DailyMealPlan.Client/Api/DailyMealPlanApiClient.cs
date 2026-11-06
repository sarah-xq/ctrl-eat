using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace DailyMealPlan.Client.Api;

public sealed class DailyMealPlanApiException(HttpStatusCode statusCode, string message)
    : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

public sealed class DailyMealPlanApiClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<DailyMealPlanResponse>> ListAsync(
        DateOnly? date,
        CancellationToken cancellationToken = default)
    {
        var plans = await SendAsync<List<DailyMealPlanResponse>>(
            HttpMethod.Get,
            date.HasValue
                ? $"api/daily-meal-plans?date={date.Value:yyyy-MM-dd}"
                : "api/daily-meal-plans",
            cancellationToken);
        return plans;
    }

    public Task<DailyMealPlanResponse> GetAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        SendAsync<DailyMealPlanResponse>(HttpMethod.Get, $"api/daily-meal-plans/{id}", cancellationToken);

    public Task<DailyMealPlanResponse> CreateAsync(
        DailyMealPlanRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<DailyMealPlanResponse>(HttpMethod.Post, "api/daily-meal-plans", request, cancellationToken);

    public Task<DailyMealPlanResponse> UpdateAsync(
        int id,
        DailyMealPlanRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<DailyMealPlanResponse>(HttpMethod.Put, $"api/daily-meal-plans/{id}", request, cancellationToken);

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Delete, $"api/daily-meal-plans/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response, cancellationToken);
        }
    }

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        CancellationToken cancellationToken) =>
        await SendAsync<T>(method, path, body: null, cancellationToken);

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        using var response = await SendAsync(method, path, body, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response, cancellationToken);
        }

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new DailyMealPlanApiException(response.StatusCode, "The API returned an empty response.");
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        CancellationToken cancellationToken) =>
        await SendAsync(method, path, body: null, cancellationToken);

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var accessToken = httpContext is null
            ? null
            : await httpContext.GetTokenAsync("access_token");
        using var request = new HttpRequestMessage(method, path);
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        try
        {
            return await httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new DailyMealPlanApiException(HttpStatusCode.ServiceUnavailable, "The meal plan service could not be reached.");
        }
    }

    private static async Task<DailyMealPlanApiException> CreateExceptionAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var detail = $"The meal plan API returned {(int)response.StatusCode} ({response.StatusCode}).";
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(JsonOptions, cancellationToken);
            if (!string.IsNullOrWhiteSpace(error?.Error))
            {
                detail = error.Error;
                if (!string.IsNullOrWhiteSpace(error.Field))
                {
                    detail = $"{error.Field}: {detail}";
                }
            }
        }
        catch (JsonException)
        {
            // Keep the status-based message when the API did not return JSON.
        }

        return new DailyMealPlanApiException(response.StatusCode, detail);
    }

    private sealed record ApiError(string? Error, string? Field);
}