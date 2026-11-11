using CtrlEat.RazorClient.Models;
using CtrlEat.RazorClient.Services;

namespace CtrlEat.RazorClient.Pages.StorageLocations;

public sealed class DetailsModel(StorageLocationApiClient api) : PageModel
{
    [BindProperty(SupportsGet = true)] public int Id { get; set; }
    public StorageLocationResponse? Location { get; private set; }
    public string? ErrorMessage { get; private set; }
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try { Location = await api.GetAsync(Id, cancellationToken); }
        catch (ApiException exception) { ErrorMessage = exception.Message; }
    }
}