using System.Net;
using DailyMealPlan.Client.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DailyMealPlan.Client.Pages.MealPlans;

public sealed class EditModel(DailyMealPlanApiClient apiClient) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int? Id { get; set; }

    [BindProperty]
    public EditorInput Input { get; set; } = new() { Date = DateOnly.FromDateTime(DateTime.Today) };

    public string? Error { get; private set; }
    public bool IsEditing => Id.HasValue;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!Id.HasValue)
        {
            return Page();
        }

        try
        {
            var plan = await apiClient.GetAsync(Id.Value, cancellationToken);
            Input = EditorInput.FromResponse(plan);
            return Page();
        }
        catch (DailyMealPlanApiException exception)
        {
            if (exception.StatusCode == HttpStatusCode.Unauthorized)
            {
                return RedirectToLogin();
            }

            Error = exception.StatusCode == HttpStatusCode.Forbidden
                ? "You are not allowed to read this meal plan."
                : exception.Message;
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        ValidateInput();
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var request = Input.ToRequest();
        try
        {
            if (Id.HasValue)
            {
                await apiClient.UpdateAsync(Id.Value, request, cancellationToken);
            }
            else
            {
                await apiClient.CreateAsync(request, cancellationToken);
            }

            return RedirectToPage("/MealPlans/Index");
        }
        catch (DailyMealPlanApiException exception)
        {
            if (exception.StatusCode == HttpStatusCode.Unauthorized)
            {
                return RedirectToLogin();
            }

            Error = exception.StatusCode == HttpStatusCode.Forbidden
                ? "You are not allowed to change this meal plan."
                : exception.Message;
            ModelState.AddModelError(string.Empty, Error);
            return Page();
        }
    }

    private void ValidateInput()
    {
        if (Input.Date == default)
        {
            ModelState.AddModelError("Input.Date", "A date is required.");
        }

        for (var index = 0; index < Input.Snacks.Count; index++)
        {
            if (Input.Snacks[index].Quantity <= 0)
            {
                ModelState.AddModelError(
                    $"Input.Snacks[{index}].Quantity",
                    "Quantity must be greater than zero.");
            }
        }
    }

    private RedirectToPageResult RedirectToLogin() =>
        RedirectToPage("/Account/Login", new
        {
            returnUrl = Request.Path + Request.QueryString
        });
}

public sealed class EditorInput
{
    public DateOnly Date { get; set; }
    public int? BreakfastRecipeId { get; set; }
    public int? LunchRecipeId { get; set; }
    public int? DinnerRecipeId { get; set; }
    public List<EditorSnackInput> Snacks { get; set; } = [];

    public DailyMealPlanRequest ToRequest() => new()
    {
        Date = Date,
        BreakfastRecipeId = BreakfastRecipeId,
        LunchRecipeId = LunchRecipeId,
        DinnerRecipeId = DinnerRecipeId,
        Snacks = Snacks.Select(snack => new PlanSnackRequest
        {
            IsRecipe = snack.IsRecipe,
            ItemId = snack.ItemId,
            Quantity = snack.Quantity,
            Unit = snack.Unit
        }).ToList()
    };

    public static EditorInput FromResponse(DailyMealPlanResponse response) => new()
    {
        Date = response.Date,
        BreakfastRecipeId = response.BreakfastRecipeId,
        LunchRecipeId = response.LunchRecipeId,
        DinnerRecipeId = response.DinnerRecipeId,
        Snacks = response.Snacks.Select(snack => new EditorSnackInput
        {
            IsRecipe = snack.IsRecipe,
            ItemId = snack.ItemId,
            Quantity = snack.Quantity,
            Unit = snack.Unit
        }).ToList()
    };
}

public sealed class EditorSnackInput
{
    public bool IsRecipe { get; set; }
    public int ItemId { get; set; }
    public decimal Quantity { get; set; } = 1;
    public string? Unit { get; set; }
}