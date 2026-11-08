using CtrlEat.RazorClient.Services;

namespace CtrlEat.RazorClient.Pages.StorageLocations;

public sealed class EditModel(StorageLocationApiClient api) : FormModel
{
    [BindProperty(SupportsGet = true)] public int Id { get; set; }
    public bool Loaded { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try { SetInput(await api.GetAsync(Id, cancellationToken)); Loaded = true; }
        catch (ApiException exception) { ErrorMessage = exception.Message; }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Loaded = true; SubmitLabel = "Save changes";
        if (!PrepareInput()) return Page();
        try { await api.UpdateAsync(Id, Input, cancellationToken); return RedirectToPage("Details", new { id = Id }); }
        catch (ApiException exception) { ErrorMessage = exception.Message; return Page(); }
    }
}