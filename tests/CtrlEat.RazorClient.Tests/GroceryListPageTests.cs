using CtrlEat.RazorClient.Models;
using CtrlEat.RazorClient.Pages.GroceryLists;
using CtrlEat.RazorClient.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CtrlEat.RazorClient.Tests;

public sealed class GroceryListPageTests
{
    [Fact]
    public async Task CreateRejectsMissingDateAndInvalidIngredientIdsWithoutCallingApi()
    {
        var model = new CreateModel(CreateApiClient());
        model.Input = new GroceryListRequest();
        model.IngredientIdsText = "1, nope, 0";

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.Contains(model.ModelState, entry => entry.Key == "Input.Date");
        Assert.Contains(model.ModelState, entry => entry.Key == nameof(model.IngredientIdsText));
    }

    private static GroceryListApiClient CreateApiClient() =>
        new(new ThrowingFactory(), new Microsoft.AspNetCore.Http.HttpContextAccessor());

    private sealed class ThrowingFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => throw new InvalidOperationException("HTTP should not be called for invalid form data.");
    }

}
