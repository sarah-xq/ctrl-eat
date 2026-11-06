using System.Net;
using DailyMealPlan.Client.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DailyMealPlan.Client.Pages.MealPlans;

public sealed class IndexModel(DailyMealPlanApiClient apiClient) : PageModel
{
    public IReadOnlyList<DailyMealPlanResponse> Plans { get; private set; } = [];
    [BindProperty(SupportsGet = true)]
    public DateOnly? Date { get; set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            Plans = await apiClient.ListAsync(Date, cancellationToken);
            return Page();
        }
        catch (DailyMealPlanApiException exception)
        {
            return HandleApiError(exception);
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await apiClient.DeleteAsync(id, cancellationToken);
            return RedirectToPage(new { date = Date?.ToString("yyyy-MM-dd") });
        }
        catch (DailyMealPlanApiException exception)
        {
            if (exception.StatusCode == HttpStatusCode.Unauthorized)
            {
                return RedirectToLogin();
            }

            Error = exception.StatusCode == HttpStatusCode.Forbidden
                ? "You are not allowed to delete this meal plan."
                : exception.Message;
            await LoadPlansAsync(cancellationToken);
            return Page();
        }
    }

    private async Task LoadPlansAsync(CancellationToken cancellationToken)
    {
        try
        {
            Plans = await apiClient.ListAsync(Date, cancellationToken);
        }
        catch (DailyMealPlanApiException exception)
        {
            Error = exception.Message;
        }
    }

    private IActionResult HandleApiError(DailyMealPlanApiException exception)
    {
        if (exception.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToLogin();
        }

        Error = exception.StatusCode == HttpStatusCode.Forbidden
            ? "Your account does not have permission to read meal plans."
            : exception.Message;
        return Page();
    }

    private RedirectToPageResult RedirectToLogin() =>
        RedirectToPage("/Account/Login", new
        {
            returnUrl = Request.Path + Request.QueryString
        });
}