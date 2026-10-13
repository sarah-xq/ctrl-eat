using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CtrlEat.Controllers;
using CtrlEat.DTOs;
using CtrlEat.Repositories.Implementations;
using CtrlEat.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CtrlEat.Tests;

public class DailyMealPlanReferenceValidationTests
{
    private const int MissingId = 999;

    [Theory]
    [InlineData(nameof(CreateDailyMealPlanRequest.BreakfastRecipeId))]
    [InlineData(nameof(CreateDailyMealPlanRequest.LunchRecipeId))]
    [InlineData(nameof(CreateDailyMealPlanRequest.DinnerRecipeId))]
    public void CreateWithMissingMealRecipeReturnsBadRequestWithoutPersistence(string field)
    {
        using var context = new TestContext();
        var request = new CreateDailyMealPlanRequest();

        switch (field)
        {
            case nameof(CreateDailyMealPlanRequest.BreakfastRecipeId):
                request.BreakfastRecipeId = MissingId;
                break;
            case nameof(CreateDailyMealPlanRequest.LunchRecipeId):
                request.LunchRecipeId = MissingId;
                break;
            case nameof(CreateDailyMealPlanRequest.DinnerRecipeId):
                request.DinnerRecipeId = MissingId;
                break;
        }

        var result = context.Controller.CreateDailyMealPlan(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        AssertErrorIncludes(badRequest, field, MissingId.ToString());
        Assert.Empty(context.MealPlanRepository.GetAll());
        Assert.Equal("[]", File.ReadAllText(context.MealPlansPath));
    }

    [Theory]
    [InlineData(true, "recipe")]
    [InlineData(false, "ingredient")]
    public void CreateWithMissingSnackReferenceReturnsBadRequestWithoutPersistence(bool isRecipe, string referenceType)
    {
        using var context = new TestContext();
        var result = context.Controller.CreateDailyMealPlan(new CreateDailyMealPlanRequest
        {
            Snacks =
            {
                new SnackRequest { IsRecipe = isRecipe, ItemId = MissingId, Quantity = 1, Unit = "serving" }
            }
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        AssertErrorIncludes(badRequest, "Snacks[0].ItemId", MissingId.ToString());
        AssertErrorIncludes(badRequest, "Snacks[0].ItemId", referenceType);
        Assert.Empty(context.MealPlanRepository.GetAll());
        Assert.Equal("[]", File.ReadAllText(context.MealPlansPath));
    }

    [Fact]
    public void CreateWithValidReferencesCalculatesCaloriesAndPersists()
    {
        using var context = new TestContext();
        var recipe = context.CreateValidRecipe(calories: 40);

        var result = context.Controller.CreateDailyMealPlan(new CreateDailyMealPlanRequest
        {
            BreakfastRecipeId = recipe.Id,
            LunchRecipeId = recipe.Id,
            DinnerRecipeId = recipe.Id,
            Snacks =
            {
                new SnackRequest { IsRecipe = true, ItemId = recipe.Id, Quantity = 0.5m, Unit = "serving" },
                new SnackRequest { IsRecipe = false, ItemId = context.ValidIngredientId, Quantity = 2, Unit = "serving" }
            }
        });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var plan = Assert.IsType<DailyMealPlanResponse>(created.Value);
        Assert.Equal(220, plan.TotalCalories);
        Assert.Single(context.MealPlanRepository.GetAll());
    }

    [Fact]
    public void CreateWithOmittedOptionalReferencesIsAccepted()
    {
        using var context = new TestContext();

        var result = context.Controller.CreateDailyMealPlan(new CreateDailyMealPlanRequest());

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var plan = Assert.IsType<DailyMealPlanResponse>(created.Value);
        Assert.Equal(0, plan.TotalCalories);
        Assert.Null(plan.BreakfastRecipeId);
        Assert.Empty(plan.Snacks);
    }

    [Fact]
    public void UpdateWithMissingRecipeReturnsBadRequestAndPreservesStoredJson()
    {
        using var context = new TestContext();
        context.DailyMealPlanService.CreateDailyMealPlan(new CreateDailyMealPlanRequest
        {
            UserId = 7,
            Date = new DateTime(2025, 1, 2),
            Snacks = { new SnackRequest { IsRecipe = false, ItemId = context.ValidIngredientId, Quantity = 1, Unit = "serving" } }
        });
        var originalJson = File.ReadAllText(context.MealPlansPath);

        var result = context.Controller.UpdateDailyMealPlan(1, new UpdateDailyMealPlanRequest
        {
            BreakfastRecipeId = MissingId
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        AssertErrorIncludes(badRequest, nameof(UpdateDailyMealPlanRequest.BreakfastRecipeId), MissingId.ToString());
        Assert.Equal(originalJson, File.ReadAllText(context.MealPlansPath));
    }

    [Fact]
    public void UpdateWithMissingSnackIngredientReturnsBadRequestAndPreservesStoredJson()
    {
        using var context = new TestContext();
        context.DailyMealPlanService.CreateDailyMealPlan(new CreateDailyMealPlanRequest
        {
            UserId = 7,
            Date = new DateTime(2025, 1, 2),
            BreakfastRecipeId = context.CreateValidRecipe().Id
        });
        var originalJson = File.ReadAllText(context.MealPlansPath);

        var result = context.Controller.UpdateDailyMealPlan(1, new UpdateDailyMealPlanRequest
        {
            Snacks = { new SnackRequest { IsRecipe = false, ItemId = MissingId, Quantity = 1, Unit = "serving" } }
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        AssertErrorIncludes(badRequest, "Snacks[0].ItemId", MissingId.ToString());
        Assert.Equal(originalJson, File.ReadAllText(context.MealPlansPath));
    }

    [Fact]
    public void UpdateWithValidReferencesSavesChangesAndCalculatesCalories()
    {
        using var context = new TestContext();
        var recipe = context.CreateValidRecipe(calories: 40);
        var created = context.DailyMealPlanService.CreateDailyMealPlan(new CreateDailyMealPlanRequest());

        var result = context.Controller.UpdateDailyMealPlan(created.Id, new UpdateDailyMealPlanRequest
        {
            BreakfastRecipeId = recipe.Id,
            Snacks = { new SnackRequest { IsRecipe = false, ItemId = context.ValidIngredientId, Quantity = 1.5m, Unit = "serving" } }
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var plan = Assert.IsType<DailyMealPlanResponse>(ok.Value);
        Assert.Equal(100, plan.TotalCalories);
        var persisted = context.MealPlanRepository.GetById(created.Id);
        Assert.Equal(recipe.Id, persisted.BreakfastRecipeId);
        Assert.Equal(100, persisted.TotalCalories);
    }

    [Fact]
    public void UpdateOfMissingPlanRemainsNotFound()
    {
        using var context = new TestContext();

        var result = context.Controller.UpdateDailyMealPlan(MissingId, new UpdateDailyMealPlanRequest
        {
            BreakfastRecipeId = MissingId
        });

        Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Empty(context.MealPlanRepository.GetAll());
    }

    private static void AssertErrorIncludes(BadRequestObjectResult result, string field, string invalidIdOrType)
    {
        var message = result.Value?.GetType().GetProperty("error", BindingFlags.Instance | BindingFlags.Public)?.GetValue(result.Value) as string;
        Assert.NotNull(message);
        Assert.Contains(field, message);
        Assert.Contains(invalidIdOrType, message);
    }

    private sealed class TestContext : IDisposable
    {
        private readonly string _directory = Directory.CreateTempSubdirectory("ctrl-eat-tests-").FullName;
        private readonly IngredientService _ingredientService;
        private readonly RecipeService _recipeService;

        public TestContext()
        {
            IngredientsPath = Path.Combine(_directory, "ingredients.json");
            RecipesPath = Path.Combine(_directory, "recipes.json");
            RecipeIngredientsPath = Path.Combine(_directory, "recipeIngredients.json");
            MealPlansPath = Path.Combine(_directory, "dailyMealPlans.json");

            _ingredientService = new IngredientService(new JsonIngredientRepository(IngredientsPath));
            _recipeService = new RecipeService(new JsonRecipeRepository(RecipesPath, RecipeIngredientsPath), _ingredientService);
            MealPlanRepository = new JsonDailyMealPlanRepository(MealPlansPath);
            DailyMealPlanService = new DailyMealPlanService(MealPlanRepository, _recipeService, _ingredientService);
            Controller = new DailyMealPlansController(DailyMealPlanService);

            ValidIngredientId = _ingredientService.CreateIngredient(new CreateIngredientRequest
            {
                Name = "Test ingredient",
                FoodGroup = "Test",
                Calories = 40
            }).Id;
        }

        public string IngredientsPath { get; }
        public string RecipesPath { get; }
        public string RecipeIngredientsPath { get; }
        public string MealPlansPath { get; }
        public int ValidIngredientId { get; }
        public JsonDailyMealPlanRepository MealPlanRepository { get; }
        public DailyMealPlanService DailyMealPlanService { get; }
        public DailyMealPlansController Controller { get; }

        public RecipeResponse CreateValidRecipe(decimal calories = 40)
        {
            if (calories != 40)
            {
                _ingredientService.UpdateIngredient(ValidIngredientId, new CreateIngredientRequest
                {
                    Name = "Test ingredient",
                    FoodGroup = "Test",
                    Calories = calories
                });
            }

            return _recipeService.CreateRecipe(new CreateRecipeRequest
            {
                Name = "Test recipe",
                TotalTimeMinutes = 10,
                CookTimeMinutes = 5,
                Ingredients = { new RecipeIngredientRequest { IngredientId = ValidIngredientId, Quantity = 1, Unit = "serving" } }
            });
        }

        public void Dispose()
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
