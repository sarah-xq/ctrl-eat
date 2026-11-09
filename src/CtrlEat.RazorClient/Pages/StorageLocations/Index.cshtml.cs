using CtrlEat.RazorClient.Models;
using CtrlEat.RazorClient.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CtrlEat.RazorClient.Pages.StorageLocations;

public sealed class IndexModel(StorageLocationApiClient api) : PageModel
{
    public IReadOnlyList<StorageLocationResponse>? Locations { get; private set; }
    public string? ErrorMessage { get; private set; }
    [BindProperty(SupportsGet = true)] public string? Name { get; set; }
    [BindProperty(SupportsGet = true)] public string? Type { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try { Locations = await api.SearchAsync(Name, Type, cancellationToken); }
        catch (ApiException exception) { ErrorMessage = exception.Message; }
    }
}