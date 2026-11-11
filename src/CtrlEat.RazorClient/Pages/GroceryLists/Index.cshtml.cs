using CtrlEat.RazorClient.Models;
using CtrlEat.RazorClient.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CtrlEat.RazorClient.Pages.GroceryLists;

public sealed class IndexModel(GroceryListApiClient api) : PageModel
{
    public IReadOnlyList<GroceryListResponse>? GroceryLists { get; private set; }
    public string? ErrorMessage { get; private set; }
    [BindProperty(SupportsGet = true)] public DateTime? Date { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try { GroceryLists = await api.GetAllAsync(Date, cancellationToken); }
        catch (GroceryListApiException exception) { ErrorMessage = exception.Message; }
    }
}
