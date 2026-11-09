using CtrlEat.RazorClient.Models;
using CtrlEat.RazorClient.Pages.StorageLocations;
using CtrlEat.RazorClient.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CtrlEat.RazorClient.Tests;

public sealed class StorageLocationPageTests
{
    [Fact]
    public void SearchPropertiesAcceptGetQueryValues()
    {
        var name = typeof(IndexModel).GetProperty(nameof(IndexModel.Name))!;
        var type = typeof(IndexModel).GetProperty(nameof(IndexModel.Type))!;

        Assert.True(name.GetCustomAttributes(typeof(BindPropertyAttribute), true).Cast<BindPropertyAttribute>().Single().SupportsGet);
        Assert.True(type.GetCustomAttributes(typeof(BindPropertyAttribute), true).Cast<BindPropertyAttribute>().Single().SupportsGet);
    }

    [Fact]
    public async Task CreateValidationRejectsMissingNamesAndReversedTemperatures()
    {
        var model = new CreateModel(CreateApiClient());
        model.Input = new StorageLocationRequest { MinimumTemperature = 10, MaximumTemperature = 1 };
        model.IngredientIdsText = string.Empty;

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.Contains(model.ModelState, entry => entry.Key == "Input.Name");
        Assert.Contains(model.ModelState, entry => entry.Key == "Input.Type");
        Assert.Contains(model.ModelState, entry => entry.Key == "Input.MinimumTemperature");
    }

    private static StorageLocationApiClient CreateApiClient() =>
        new(new ThrowingFactory(), new HttpContextAccessor { HttpContext = new DefaultHttpContext() });

    private sealed class ThrowingFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => throw new InvalidOperationException("HTTP should not be called for invalid form data.");
    }
}