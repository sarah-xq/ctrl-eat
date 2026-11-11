using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DailyMealPlan.Client.Pages.Account;

public sealed class LoginModel : PageModel
{
    public IActionResult OnGet(string? returnUrl = null)
    {
        var target = Url.IsLocalUrl(returnUrl) ? returnUrl : "/MealPlans";
        return Challenge(
            new AuthenticationProperties { RedirectUri = target },
            OpenIdConnectDefaults.AuthenticationScheme);
    }
}