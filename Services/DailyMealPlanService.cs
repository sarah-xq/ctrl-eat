using System;
using System.Collections.Generic;
using System.Linq;
using CtrlEat.DTOs;
using CtrlEat.Models;
using CtrlEat.Repositories.Interfaces;

namespace CtrlEat.Services
{
    /// <summary>
    /// Business logic for Daily Meal Plans
    /// Communicates with RecipeService and IngredientService to calculate calories and validate references
    /// </summary>
    public class DailyMealPlanService
    {
        private readonly IDailyMealPlanRepository _repository;
        private readonly RecipeService _recipeService;
        private readonly IngredientService _ingredientService;

        public DailyMealPlanService(IDailyMealPlanRepository repository, RecipeService recipeService, IngredientService ingredientService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
            _ingredientService = ingredientService ?? throw new ArgumentNullException(nameof(ingredientService));
        }

        public DailyMealPlanResponse CreateDailyMealPlan(CreateDailyMealPlanRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var plan = new DailyMealPlan
            {
                UserId = request.UserId,
                BreakfastRecipeId = request.BreakfastRecipeId,
                LunchRecipeId = request.LunchRecipeId,
                DinnerRecipeId = request.DinnerRecipeId,
                Date = request.Date.Date,
                Snacks = request.Snacks?.Select(s => new SnackItem
                {
                    IsRecipe = s.IsRecipe,
                    ItemId = s.ItemId,
                    Quantity = s.Quantity,
                    Unit = s.Unit
                }).ToList() ?? new List<SnackItem>()
            };

            plan.TotalCalories = CalculateTotalCalories(plan);

            var id = _repository.Create(plan);
            plan.Id = id;

            return MapToResponse(plan);
        }

        public DailyMealPlanResponse GetDailyMealPlan(int id)
        {
            var plan = _repository.GetById(id);
            if (plan == null) throw new InvalidOperationException($"DailyMealPlan with id {id} not found");
            return MapToResponse(plan);
        }

        public List<DailyMealPlanResponse> SearchDailyMealPlans(GetDailyMealPlansRequest request)
        {
            var results = new List<DailyMealPlan>();
            if (request == null) request = new GetDailyMealPlansRequest();

            if (request.Id.HasValue)
            {
                var p = _repository.GetById(request.Id.Value);
                if (p != null) results.Add(p);
            }
            else if (request.UserId.HasValue)
            {
                results = _repository.GetByUserId(request.UserId.Value);
            }
            else
            {
                results = _repository.GetAll();
            }

            if (request.Date.HasValue)
            {
                results = results.Where(r => r.Date.Date == request.Date.Value.Date).ToList();
            }

            return results.Select(MapToResponse).ToList();
        }

        public DailyMealPlanResponse UpdateDailyMealPlan(int id, UpdateDailyMealPlanRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var existing = _repository.GetById(id);
            if (existing == null) throw new InvalidOperationException($"DailyMealPlan with id {id} not found");

            if (request.BreakfastRecipeId.HasValue) existing.BreakfastRecipeId = request.BreakfastRecipeId;
            if (request.LunchRecipeId.HasValue) existing.LunchRecipeId = request.LunchRecipeId;
            if (request.DinnerRecipeId.HasValue) existing.DinnerRecipeId = request.DinnerRecipeId;
            if (request.Date.HasValue) existing.Date = request.Date.Value.Date;

            existing.Snacks = request.Snacks?.Select(s => new SnackItem
            {
                IsRecipe = s.IsRecipe,
                ItemId = s.ItemId,
                Quantity = s.Quantity,
                Unit = s.Unit
            }).ToList() ?? new List<SnackItem>();

            existing.TotalCalories = CalculateTotalCalories(existing);

            _repository.Update(existing);
            return MapToResponse(existing);
        }

        public void DeleteDailyMealPlan(int id)
        {
            _repository.Delete(id);
        }

        private decimal CalculateTotalCalories(DailyMealPlan plan)
        {
            decimal total = 0;

            if (plan.BreakfastRecipeId.HasValue)
            {
                var r = _recipeService.GetRecipe(plan.BreakfastRecipeId.Value);
                total += r.TotalCalories;
            }
            if (plan.LunchRecipeId.HasValue)
            {
                var r = _recipeService.GetRecipe(plan.LunchRecipeId.Value);
                total += r.TotalCalories;
            }
            if (plan.DinnerRecipeId.HasValue)
            {
                var r = _recipeService.GetRecipe(plan.DinnerRecipeId.Value);
                total += r.TotalCalories;
            }

            foreach (var snack in plan.Snacks ?? Enumerable.Empty<SnackItem>())
            {
                if (snack.IsRecipe)
                {
                    try
                    {
                        var r = _recipeService.GetRecipe(snack.ItemId);
                        total += r.TotalCalories * snack.Quantity;
                    }
                    catch
                    {
                        // ignore missing recipe
                    }
                }
                else
                {
                    try
                    {
                        var ing = _ingredientService.GetIngredientForLookup(snack.ItemId);
                        total += ing.Calories * snack.Quantity;
                    }
                    catch
                    {
                        // ignore missing ingredient
                    }
                }
            }

            return total;
        }

        private DailyMealPlanResponse MapToResponse(DailyMealPlan plan)
        {
            var response = new DailyMealPlanResponse
            {
                Id = plan.Id,
                UserId = plan.UserId,
                BreakfastRecipeId = plan.BreakfastRecipeId,
                LunchRecipeId = plan.LunchRecipeId,
                DinnerRecipeId = plan.DinnerRecipeId,
                Date = plan.Date,
                TotalCalories = plan.TotalCalories,
                CreatedAt = plan.CreatedAt
            };

            foreach (var s in plan.Snacks ?? Enumerable.Empty<SnackItem>())
            {
                var snackResp = new SnackResponse
                {
                    IsRecipe = s.IsRecipe,
                    ItemId = s.ItemId,
                    Quantity = s.Quantity,
                    Unit = s.Unit,
                    Name = null,
                    Calories = 0
                };

                if (s.IsRecipe)
                {
                    try
                    {
                        var r = _recipeService.GetRecipe(s.ItemId);
                        snackResp.Name = r.Name;
                        snackResp.Calories = r.TotalCalories * s.Quantity;
                    }
                    catch { }
                }
                else
                {
                    try
                    {
                        var ing = _ingredientService.GetIngredientForLookup(s.ItemId);
                        snackResp.Name = ing.Name;
                        snackResp.Calories = ing.Calories * s.Quantity;
                    }
                    catch { }
                }

                response.Snacks.Add(snackResp);
            }

            return response;
        }
    }
}
