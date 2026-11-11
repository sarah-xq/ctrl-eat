using System.Net.Http.Headers;

namespace DailyMealPlan.Api.Clients;

public sealed class DownstreamBearerHandler(IHttpContextAccessor context) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null && context.HttpContext?.User.Identity?.IsAuthenticated == true &&
            AuthenticationHeaderValue.TryParse(context.HttpContext.Request.Headers.Authorization.ToString(), out var auth) &&
            string.Equals(auth.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase))
            request.Headers.Authorization = auth;
        return base.SendAsync(request, cancellationToken);
    }
}
