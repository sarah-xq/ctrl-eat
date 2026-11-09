using GroceryList.Api.Clients;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GroceryList.Api.Services;

public sealed class GroceryListExceptionHandler(ILogger<GroceryListExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.RequestAborted.IsCancellationRequested && exception is OperationCanceledException)
        {
            return false;
        }

        var (statusCode, title) = exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid grocery list request."),
            IngredientNotFoundException => (StatusCodes.Status400BadRequest, "Ingredient does not exist."),
            IngredientLookupUnavailableException => (StatusCodes.Status503ServiceUnavailable, "Ingredient service is unavailable."),
            IngredientUpstreamAuthorizationException => (StatusCodes.Status502BadGateway, "Ingredient service authorization failed."),
            _ => (0, string.Empty)
        };

        if (statusCode == 0)
        {
            return false;
        }

        logger.LogWarning(exception, "Grocery List request failed with status code {StatusCode}.", statusCode);
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title
        }, cancellationToken);
        return true;
    }
}
