using CtrlEat.RazorClient.Models;
using CtrlEat.RazorClient.Services;

namespace CtrlEat.RazorClient.Pages.StorageLocations;

public sealed class DeleteModel(StorageLocationApiClient api) : PageModel
{
    [BindProperty(SupportsGet = true)] public int Id { get; set; }
    public StorageLocationResponse? Location { get; private set; }
    public string? ErrorMessage { get; private set; }
    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try { await api.DeleteAsync(Id, cancellationToken); return RedirectToPage("Index"); }
        catch (ApiException exception) { ErrorMessage = exception.Message; await LoadAsync(cancellationToken); return Page(); }
    }
    private async Task LoadAsync(CancellationToken cancellationToken) { try { Location = await api.GetAsync(Id, cancellationToken); } catch (ApiException exception) { ErrorMessage = exception.Message; } }
}