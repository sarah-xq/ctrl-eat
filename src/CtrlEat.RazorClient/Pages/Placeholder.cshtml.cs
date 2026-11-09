using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CtrlEat.RazorClient.Pages;

public sealed class PlaceholderModel : PageModel
{
    [BindProperty(SupportsGet = true)] public string AreaName { get; set; } = "Service area";
}