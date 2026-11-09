using System;
using System.Collections.Generic;
using System.IO;
using CtrlEat.Controllers;
using CtrlEat.DTOs;
using CtrlEat.Models;
using CtrlEat.Repositories.Implementations;
using CtrlEat.Services;
using Microsoft.AspNetCore.Mvc;

namespace CtrlEat.Tests;

public sealed class GroceryListServiceTests
{
    private const int MissingIngredientId = 999;
    private const int MissingGroceryListId = 999;

    [Fact]
    public void CreateGetUpdateAndDeleteGroceryList()
    {
        using var context = new TestContext();
        var ingredientId = context.CreateIngredient("Milk", "Dairy", 61);
        var createDate = new DateTime(2026, 11, 12, 14, 30, 0, DateTimeKind.Local);

        var createResult = context.Controller.CreateGroceryList(Request(createDate, [ingredientId]));

        var createdAtAction = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var created = Assert.IsType<GroceryListResponse>(createdAtAction.Value);
        Assert.Equal(nameof(GroceryListsController.GetGroceryList), createdAtAction.ActionName);
        Assert.Equal(created.Id, context.Service.GetGroceryList(created.Id).Id);
        Assert.Equal(createDate.Date, created.Date);
        var enrichedIngredient = Assert.Single(created.Ingredients);
        Assert.Equal(ingredientId, enrichedIngredient.Id);
        Assert.Equal("Milk", enrichedIngredient.Name);
        Assert.Equal("Dairy", enrichedIngredient.FoodGroup);
        Assert.Equal(61, enrichedIngredient.Calories);

        var persistedAfterCreate = context.Repository.GetById(created.Id)!;
        Assert.Equal(createDate.Date, persistedAfterCreate.Date);
        Assert.Equal(created.CreatedAt, persistedAfterCreate.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, persistedAfterCreate.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, persistedAfterCreate.UpdatedAt.Kind);
        Assert.True(persistedAfterCreate.CreatedAt <= persistedAfterCreate.UpdatedAt);

        var updateDate = new DateTime(2026, 11, 13, 22, 45, 0, DateTimeKind.Local);
        var updatedResult = context.Controller.UpdateGroceryList(created.Id, Request(updateDate, []));
        var updated = Assert.IsType<GroceryListResponse>(Assert.IsType<OkObjectResult>(updatedResult.Result).Value);
        Assert.Equal(updateDate.Date, updated.Date);
        Assert.Empty(updated.Ingredients);

        var persistedAfterUpdate = context.Repository.GetById(created.Id)!;
        Assert.Equal(persistedAfterCreate.CreatedAt, persistedAfterUpdate.CreatedAt);
        Assert.True(persistedAfterUpdate.UpdatedAt > persistedAfterCreate.UpdatedAt);

        Assert.IsType<NoContentResult>(context.Controller.DeleteGroceryList(created.Id));
        Assert.Null(context.Repository.GetById(created.Id));
    }

    [Fact]
    public void ListFiltersByDateIgnoringTimeOfDay()
    {
        using var context = new TestContext();
        var wantedDate = new DateTime(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc);
        var first = context.Service.CreateGroceryList(Request(wantedDate.AddHours(8), []));
        var second = context.Service.CreateGroceryList(Request(wantedDate.AddHours(21), []));
        context.Service.CreateGroceryList(Request(wantedDate.AddDays(1), []));

        var results = context.Service.SearchGroceryLists(new GetGroceryListsRequest { Date = wantedDate.AddHours(17) });

        Assert.Equal(new[] { first.Id, second.Id }, results.ConvertAll(list => list.Id));
        Assert.All(results, list => Assert.Equal(wantedDate.Date, list.Date));
    }

    [Fact]
    public void CreateAllowsEmptyIngredientList()
    {
        using var context = new TestContext();

        var created = context.Service.CreateGroceryList(Request(new DateTime(2026, 11, 12), []));

        Assert.Empty(created.Ingredients);
        Assert.Empty(context.Repository.GetById(created.Id)!.IngredientIds);
    }

    [Fact]
    public void CreateTreatsNullIngredientListAsEmpty()
    {
        using var context = new TestContext();

        var created = context.Service.CreateGroceryList(Request(new DateTime(2026, 11, 12), null));

        Assert.Empty(created.Ingredients);
        Assert.Empty(context.Repository.GetById(created.Id)!.IngredientIds);
    }

    [Fact]
    public void CreateRemovesDuplicateIngredientIdsAndReturnsEnrichedDetails()
    {
        using var context = new TestContext();
        var ingredientId = context.CreateIngredient("Apples", "Fruit", 52);

        var created = context.Service.CreateGroceryList(Request(
            new DateTime(2026, 11, 12), [ingredientId, ingredientId]));

        var ingredient = Assert.Single(created.Ingredients);
        Assert.Equal(ingredientId, ingredient.Id);
        Assert.Equal("Apples", ingredient.Name);
        Assert.Equal("Fruit", ingredient.FoodGroup);
        Assert.Equal(52, ingredient.Calories);
        Assert.Equal(new[] { ingredientId }, context.Repository.GetById(created.Id)!.IngredientIds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateRejectsNonpositiveIngredientIdsWithoutPersistence(int ingredientId)
    {
        using var context = new TestContext();

        var result = context.Controller.CreateGroceryList(Request(new DateTime(2026, 11, 12), [ingredientId]));

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(context.Repository.GetAll());
    }

    [Fact]
    public void CreateRejectsDefaultOrMissingDateWithoutPersistence()
    {
        using var context = new TestContext();

        var result = context.Controller.CreateGroceryList(new CreateGroceryListRequest { IngredientIds = [] });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(context.Repository.GetAll());
    }

    [Fact]
    public void CreateRejectsIngredientThatDoesNotExist()
    {
        using var context = new TestContext();

        var result = context.Controller.CreateGroceryList(Request(
            new DateTime(2026, 11, 12), [MissingIngredientId]));

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(context.Repository.GetAll());
    }

    [Fact]
    public void UpdateWithMissingIngredientReturnsNotFoundAndPreservesStoredList()
    {
        using var context = new TestContext();
        var original = context.Service.CreateGroceryList(Request(new DateTime(2026, 11, 12), []));

        var result = context.Controller.UpdateGroceryList(original.Id, Request(
            new DateTime(2026, 11, 13), [MissingIngredientId]));

        // Legacy controller maps every InvalidOperationException from the service to 404 on update.
        Assert.IsType<NotFoundObjectResult>(result.Result);
        var persisted = context.Repository.GetById(original.Id)!;
        Assert.Equal(original.Date, persisted.Date);
        Assert.Empty(persisted.IngredientIds);
    }

    [Fact]
    public void GetUpdateAndDeleteMissingGroceryListReturnNotFound()
    {
        using var context = new TestContext();

        Assert.IsType<NotFoundObjectResult>(context.Controller.GetGroceryList(MissingGroceryListId).Result);
        Assert.IsType<NotFoundObjectResult>(context.Controller.UpdateGroceryList(
            MissingGroceryListId, Request(new DateTime(2026, 11, 12), [])).Result);
        Assert.IsType<NotFoundObjectResult>(context.Controller.DeleteGroceryList(MissingGroceryListId));
    }

    private static CreateGroceryListRequest Request(DateTime date, List<int>? ingredientIds) => new()
    {
        Date = date,
        IngredientIds = ingredientIds!
    };

    private sealed class TestContext : IDisposable
    {
        private readonly string _directory = Directory.CreateTempSubdirectory("ctrl-eat-grocery-list-tests-").FullName;
        private readonly IngredientService _ingredientService;

        public TestContext()
        {
            _ingredientService = new IngredientService(
                new JsonIngredientRepository(Path.Combine(_directory, "ingredients.json")));
            Repository = new JsonGroceryListRepository(Path.Combine(_directory, "groceryLists.json"));
            Service = new GroceryListService(Repository, _ingredientService);
            Controller = new GroceryListsController(Service);
        }

        public JsonGroceryListRepository Repository { get; }
        public GroceryListService Service { get; }
        public GroceryListsController Controller { get; }

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