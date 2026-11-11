using CtrlEat.RazorClient.Models;
using CtrlEat.RazorClient.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CtrlEat.RazorClient.Pages.GroceryLists;

public sealed class DetailsModel(GroceryListApiClient api) : PageModel
{
    [BindProperty(SupportsGet = true)] public int Id { get; set; }
    public GroceryListResponse? GroceryList { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try { GroceryList = await api.GetAsync(Id, cancellationToken); }
        catch (GroceryListApiException exception) { ErrorMessage = exception.Message; }
    }
}
