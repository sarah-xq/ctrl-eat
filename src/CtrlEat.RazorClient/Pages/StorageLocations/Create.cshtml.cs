using CtrlEat.RazorClient.Services;

namespace CtrlEat.RazorClient.Pages.StorageLocations;

public sealed class CreateModel(StorageLocationApiClient api) : FormModel
{
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        SubmitLabel = "Create";
        if (!PrepareInput()) return Page();
        try { var result = await api.CreateAsync(Input, cancellationToken); return RedirectToPage("Details", new { id = result.Id }); }
        catch (ApiException exception) { ErrorMessage = exception.Message; return Page(); }
    }
}