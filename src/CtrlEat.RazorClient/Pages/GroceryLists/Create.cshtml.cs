using CtrlEat.RazorClient.Services;
using Microsoft.AspNetCore.Mvc;

namespace CtrlEat.RazorClient.Pages.GroceryLists;

public sealed class CreateModel(GroceryListApiClient api) : FormModel
{
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        SubmitLabel = "Create";
        if (!PrepareInput()) return Page();
        try
        {
            var created = await api.CreateAsync(Input, cancellationToken);
            return RedirectToPage("Details", new { id = created.Id });
        }
        catch (GroceryListApiException exception)
        {
            ErrorMessage = exception.Message;
            return Page();
        }
    }
}
