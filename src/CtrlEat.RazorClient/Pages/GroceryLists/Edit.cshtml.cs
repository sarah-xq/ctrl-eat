using CtrlEat.RazorClient.Services;
using Microsoft.AspNetCore.Mvc;

namespace CtrlEat.RazorClient.Pages.GroceryLists;

public sealed class EditModel(GroceryListApiClient api) : FormModel
{
    [BindProperty(SupportsGet = true)] public int Id { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try { SetInput(await api.GetAsync(Id, cancellationToken)); }
        catch (GroceryListApiException exception) { ErrorMessage = exception.Message; Loaded = false; }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        SubmitLabel = "Save changes";
        if (!PrepareInput()) return Page();
        try
        {
            await api.UpdateAsync(Id, Input, cancellationToken);
            return RedirectToPage("Details", new { id = Id });
        }
        catch (GroceryListApiException exception)
        {
            ErrorMessage = exception.Message;
            return Page();
        }
    }
}
