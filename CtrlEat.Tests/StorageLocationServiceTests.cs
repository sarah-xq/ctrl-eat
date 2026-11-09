using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CtrlEat.Controllers;
using CtrlEat.DTOs;
using CtrlEat.Repositories.Implementations;
using CtrlEat.Services;
using Microsoft.AspNetCore.Mvc;

namespace CtrlEat.Tests;

public sealed class StorageLocationServiceTests
{
    private const int MissingIngredientId = 999;
    private const int MissingLocationId = 999;

    [Theory]
    [InlineData(" ", "Fridge")]
    [InlineData("Pantry", "\t")]
    public void CreateWithMissingNameOrTypeIsRejectedWithoutPersistence(string name, string type)
    {
        using var context = new TestContext();

        Assert.Throws<ArgumentException>(() => context.Service.CreateStorageLocation(Request(name, type)));

        Assert.Empty(context.Repository.GetAll());
        Assert.Equal("[]", File.ReadAllText(context.StorageLocationsPath));
    }

    [Fact]
    public void CreateWithMinimumTemperatureAboveMaximumIsRejectedWithoutPersistence()
    {
        using var context = new TestContext();

        Assert.Throws<ArgumentException>(() => context.Service.CreateStorageLocation(Request(
            minimumTemperature: 5,
            maximumTemperature: 4)));

        Assert.Empty(context.Repository.GetAll());
        Assert.Equal("[]", File.ReadAllText(context.StorageLocationsPath));
    }

    [Fact]
    public void CreateRemovesDuplicateIngredientIdsAndEnrichesResponse()
    {
        using var context = new TestContext();
        var ingredientId = context.CreateIngredient("Milk", "Dairy", 61);

        var created = context.Service.CreateStorageLocation(Request(
            name: "  Main fridge  ",
            type: "  Cold  ",
            minimumTemperature: 1,
            maximumTemperature: 5,
            ingredientIds: [ingredientId, ingredientId]));

        Assert.Equal("Main fridge", created.Name);
        Assert.Equal("Cold", created.Type);
        Assert.Single(created.Ingredients);
        var ingredient = Assert.Single(created.Ingredients);
        Assert.Equal(ingredientId, ingredient.Id);
        Assert.Equal("Milk", ingredient.Name);
        Assert.Equal("Dairy", ingredient.FoodGroup);
        Assert.Equal(61, ingredient.Calories);

        var persisted = Assert.Single(context.Repository.GetAll());
        Assert.Equal(new[] { ingredientId }, persisted.IngredientIds);
    }

    [Fact]
    public void CreateWithMissingIngredientReturnsBadRequestWithoutPersistence()
    {
        using var context = new TestContext();

        var result = context.Controller.CreateStorageLocation(Request(ingredientIds: [MissingIngredientId]));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Ingredient", ErrorMessage(badRequest), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(context.Repository.GetAll());
        Assert.Equal("[]", File.ReadAllText(context.StorageLocationsPath));
    }

    [Fact]
    public void UpdateWithMissingIngredientReturnsNotFoundAndPreservesStoredLocation()
    {
        using var context = new TestContext();
        var created = context.Service.CreateStorageLocation(Request(name: "Original"));
        var originalJson = File.ReadAllText(context.StorageLocationsPath);

        var result = context.Controller.UpdateStorageLocation(
            created.Id,
            Request(name: "Changed", ingredientIds: [MissingIngredientId]));

        Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(originalJson, File.ReadAllText(context.StorageLocationsPath));
        Assert.Equal("Original", context.Repository.GetById(created.Id)?.Name);
    }

    [Fact]
    public void CreateGetUpdateAndDeleteStorageLocation()
    {
        using var context = new TestContext();
        var ingredientId = context.CreateIngredient("Apples", "Fruit", 52);

        var createResult = context.Controller.CreateStorageLocation(Request(
            name: "Fruit drawer",
            type: "Cold",
            minimumTemperature: 2,
            maximumTemperature: 6,
            ingredientIds: [ingredientId]));

        var createdAtAction = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var created = Assert.IsType<StorageLocationResponse>(createdAtAction.Value);
        Assert.Equal(created.Id, context.Service.GetStorageLocation(created.Id).Id);

        var updateResult = context.Controller.UpdateStorageLocation(created.Id, Request(
            name: "Updated drawer",
            type: "Chilled",
            minimumTemperature: 3,
            maximumTemperature: 7,
            ingredientIds: [ingredientId]));

        var updated = Assert.IsType<StorageLocationResponse>(Assert.IsType<OkObjectResult>(updateResult.Result).Value);
        Assert.Equal("Updated drawer", updated.Name);
        Assert.Equal("Chilled", updated.Type);
        Assert.Equal(3, updated.MinimumTemperature);
        Assert.Equal(7, updated.MaximumTemperature);
        Assert.Equal("Apples", Assert.Single(updated.Ingredients).Name);

        var deleteResult = context.Controller.DeleteStorageLocation(created.Id);

        Assert.IsType<NoContentResult>(deleteResult);
        Assert.Empty(context.Repository.GetAll());
    }

    [Fact]
    public void GetUpdateAndDeleteMissingStorageLocationReturnNotFound()
    {
        using var context = new TestContext();

        Assert.IsType<NotFoundObjectResult>(context.Controller.GetStorageLocation(MissingLocationId).Result);
        Assert.IsType<NotFoundObjectResult>(context.Controller.UpdateStorageLocation(
            MissingLocationId, Request()).Result);
        Assert.IsType<NotFoundObjectResult>(context.Controller.DeleteStorageLocation(MissingLocationId));
    }

    [Fact]
    public void ListCanFilterByNameAndType()
    {
        using var context = new TestContext();
        context.Service.CreateStorageLocation(Request(name: "Main fridge", type: "Cold"));
        context.Service.CreateStorageLocation(Request(name: "Chest freezer", type: "Frozen"));
        context.Service.CreateStorageLocation(Request(name: "Pantry", type: "Dry"));

        var byName = context.Controller.GetStorageLocations(name: "FRIDge");
        var byType = context.Controller.GetStorageLocations(type: "frozen");

        var nameResults = Assert.IsType<List<StorageLocationResponse>>(
            Assert.IsType<OkObjectResult>(byName.Result).Value);
        var typeResults = Assert.IsType<List<StorageLocationResponse>>(
            Assert.IsType<OkObjectResult>(byType.Result).Value);

        Assert.Equal("Main fridge", Assert.Single(nameResults).Name);
        Assert.Equal("Chest freezer", Assert.Single(typeResults).Name);
    }

    private static CreateStorageLocationRequest Request(
        string name = "Test location",
        string type = "Cold",
        decimal minimumTemperature = 1,
        decimal maximumTemperature = 5,
        List<int>? ingredientIds = null) => new()
        {
            Name = name,
            Type = type,
            MinimumTemperature = minimumTemperature,
            MaximumTemperature = maximumTemperature,
            IngredientIds = ingredientIds ?? []
        };

    private static string ErrorMessage(BadRequestObjectResult result) =>
        result.Value?.GetType().GetProperty("error")?.GetValue(result.Value) as string ?? string.Empty;

    private sealed class TestContext : IDisposable
    {
        private readonly string _directory = Directory.CreateTempSubdirectory("ctrl-eat-storage-location-tests-").FullName;
        private readonly IngredientService _ingredientService;

        public TestContext()
        {
            var ingredientsPath = Path.Combine(_directory, "ingredients.json");
            StorageLocationsPath = Path.Combine(_directory, "storageLocations.json");

            _ingredientService = new IngredientService(new JsonIngredientRepository(ingredientsPath));
            Repository = new JsonStorageLocationRepository(StorageLocationsPath);
            Service = new StorageLocationService(Repository, _ingredientService);
            Controller = new StorageLocationsController(Service);
        }

        public string StorageLocationsPath { get; }
        public JsonStorageLocationRepository Repository { get; }
        public StorageLocationService Service { get; }
        public StorageLocationsController Controller { get; }

        public int CreateIngredient(string name, string foodGroup, decimal calories) =>
            _ingredientService.CreateIngredient(new CreateIngredientRequest
            {
                Name = name,
                FoodGroup = foodGroup,
                Calories = calories
            }).Id;

        public void Dispose() => Directory.Delete(_directory, recursive: true);
    }
}