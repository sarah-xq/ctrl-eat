using System;
using System.Collections.Generic;
using System.Linq;
using CtrlEat.DTOs;
using CtrlEat.Models;
using CtrlEat.Repositories.Interfaces;

namespace CtrlEat.Services
{
    /// <summary>
    /// Business logic service for Recipe management
    /// Handles validation, transformation, and orchestration of recipe operations
    /// Communicates with Ingredient Service for ingredient lookups
    /// Separates business logic from data access
    /// </summary>
    public class RecipeService
    {
        private readonly IRecipeRepository _repository;
        private readonly IngredientService _ingredientService;

        public RecipeService(IRecipeRepository repository, IngredientService ingredientService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _ingredientService = ingredientService ?? throw new ArgumentNullException(nameof(ingredientService));
        }

        /// <summary>
        /// Create a new recipe with ingredients
        /// </summary>
        public RecipeResponse CreateRecipe(CreateRecipeRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            ValidateRecipeRequest(request);

            // Validate and fetch ingredient details for total calories calculation
            var recipeIngredients = new List<RecipeIngredient>();
            decimal totalCalories = 0;

            foreach (var ingredientRequest in request.Ingredients)
            {
                // Verify ingredient exists via inter-service call
                var ingredientLookup = _ingredientService.GetIngredientForLookup(ingredientRequest.IngredientId);

                var recipeIngredient = new RecipeIngredient
                {
                    IngredientId = ingredientRequest.IngredientId,
                    Quantity = ingredientRequest.Quantity,
                    Unit = ingredientRequest.Unit
                };
                recipeIngredients.Add(recipeIngredient);

                // Calculate total calories (ingredient calories * quantity normalized to base unit)
                totalCalories += ingredientLookup.Calories * ingredientRequest.Quantity;
            }

            var recipe = new Recipe
            {
                Name = request.Name.Trim(),
                Author = string.IsNullOrWhiteSpace(request.Author) ? null : request.Author.Trim(),
                TotalTimeMinutes = request.TotalTimeMinutes,
                CookTimeMinutes = request.CookTimeMinutes,
                Description = request.Description,
                TotalCalories = totalCalories,
                RecipeIngredients = recipeIngredients
            };

            int recipeId = _repository.Create(recipe);
            recipe.Id = recipeId;

            // Add ingredients to recipe
            foreach (var recipeIngredient in recipeIngredients)
            {
                _repository.AddIngredient(recipeId, recipeIngredient);
            }

            return MapToResponse(recipe);
        }

        /// <summary>
        /// Get recipe by ID with all ingredients enriched
        /// </summary>
        public RecipeResponse GetRecipe(int id)
        {
            var recipe = _repository.GetById(id);
            if (recipe == null)
                throw new InvalidOperationException($"Recipe with id {id} not found");

            return MapToResponse(recipe);
        }

        /// <summary>
        /// Search for recipes
        /// </summary>
        public List<RecipeResponse> SearchRecipes(GetRecipesRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            List<Recipe> results = new List<Recipe>();

            // Search by ID if provided
            if (request.Id.HasValue)
            {
                var recipe = _repository.GetById(request.Id.Value);
                if (recipe != null)
                    results.Add(recipe);
            }
            // Search by name if provided
            else if (!string.IsNullOrWhiteSpace(request.Name))
            {
                results = _repository.GetByName(request.Name);
            }
            // Search by author if provided
            else if (!string.IsNullOrWhiteSpace(request.Author))
            {
                results = _repository.GetByAuthor(request.Author);
            }
            // Get all if no criteria provided
            else
            {
                results = _repository.GetAll();
            }

            return results.Select(MapToResponse).ToList();
        }

        /// <summary>
        /// Update an existing recipe
        /// </summary>
        public RecipeResponse UpdateRecipe(int id, CreateRecipeRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            ValidateRecipeRequest(request);

            var recipe = _repository.GetById(id);
            if (recipe == null)
                throw new InvalidOperationException($"Recipe with id {id} not found");

            // Recalculate ingredients and calories
            var recipeIngredients = new List<RecipeIngredient>();
            decimal totalCalories = 0;

            foreach (var ingredientRequest in request.Ingredients)
            {
                var ingredientLookup = _ingredientService.GetIngredientForLookup(ingredientRequest.IngredientId);

                var recipeIngredient = new RecipeIngredient
                {
                    IngredientId = ingredientRequest.IngredientId,
                    Quantity = ingredientRequest.Quantity,
                    Unit = ingredientRequest.Unit
                };
                recipeIngredients.Add(recipeIngredient);

                totalCalories += ingredientLookup.Calories * ingredientRequest.Quantity;
            }

            recipe.Name = request.Name.Trim();
            recipe.Author = string.IsNullOrWhiteSpace(request.Author) ? null : request.Author.Trim();
            recipe.TotalTimeMinutes = request.TotalTimeMinutes;
            recipe.CookTimeMinutes = request.CookTimeMinutes;
            recipe.Description = request.Description;
            recipe.TotalCalories = totalCalories;
            recipe.RecipeIngredients = recipeIngredients;

            _repository.Update(recipe);

            // Update ingredients association
            var existingIngredients = _repository.GetRecipeIngredients(id);
            foreach (var existing in existingIngredients)
            {
                _repository.RemoveIngredient(id, existing.IngredientId);
            }

            foreach (var recipeIngredient in recipeIngredients)
            {
                _repository.AddIngredient(id, recipeIngredient);
            }

            return MapToResponse(recipe);
        }

        /// <summary>
        /// Delete a recipe
        /// </summary>
        public void DeleteRecipe(int id)
        {
            var recipe = _repository.GetById(id);
            if (recipe == null)
                throw new InvalidOperationException($"Recipe with id {id} not found");

            _repository.Delete(id);
        }

        /// <summary>
        /// Add an ingredient to an existing recipe
        /// </summary>
        public RecipeResponse AddIngredientToRecipe(int recipeId, RecipeIngredientRequest ingredientRequest)
        {
            if (ingredientRequest == null)
                throw new ArgumentNullException(nameof(ingredientRequest));

            var recipe = _repository.GetById(recipeId);
            if (recipe == null)
                throw new InvalidOperationException($"Recipe with id {recipeId} not found");

            // Verify ingredient exists
            var ingredientLookup = _ingredientService.GetIngredientForLookup(ingredientRequest.IngredientId);

            var recipeIngredient = new RecipeIngredient
            {
                IngredientId = ingredientRequest.IngredientId,
                Quantity = ingredientRequest.Quantity,
                Unit = ingredientRequest.Unit
            };

            _repository.AddIngredient(recipeId, recipeIngredient);

            // Recalculate total calories
            var updatedRecipe = _repository.GetById(recipeId);
            updatedRecipe.TotalCalories = CalculateTotalCalories(updatedRecipe.RecipeIngredients);
            _repository.Update(updatedRecipe);

            return MapToResponse(updatedRecipe);
        }

        /// <summary>
        /// Remove an ingredient from a recipe
        /// </summary>
        public RecipeResponse RemoveIngredientFromRecipe(int recipeId, int ingredientId)
        {
            var recipe = _repository.GetById(recipeId);
            if (recipe == null)
                throw new InvalidOperationException($"Recipe with id {recipeId} not found");

            _repository.RemoveIngredient(recipeId, ingredientId);

            // Recalculate total calories
            var updatedRecipe = _repository.GetById(recipeId);
            updatedRecipe.TotalCalories = CalculateTotalCalories(updatedRecipe.RecipeIngredients);
            _repository.Update(updatedRecipe);

            return MapToResponse(updatedRecipe);
        }

        /// <summary>
        /// Validate recipe request data
        /// </summary>
        private void ValidateRecipeRequest(CreateRecipeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Recipe name is required", nameof(request.Name));

            if (request.TotalTimeMinutes <= 0)
                throw new ArgumentException("Total time must be greater than 0", nameof(request.TotalTimeMinutes));

            if (request.CookTimeMinutes < 0)
                throw new ArgumentException("Cook time cannot be negative", nameof(request.CookTimeMinutes));

            if (request.CookTimeMinutes > request.TotalTimeMinutes)
                throw new ArgumentException("Cook time cannot exceed total time", nameof(request.CookTimeMinutes));

            if (request.Ingredients == null || request.Ingredients.Count == 0)
                throw new ArgumentException("Recipe must contain at least one ingredient", nameof(request.Ingredients));

            foreach (var ingredient in request.Ingredients)
            {
                if (ingredient.IngredientId <= 0)
                    throw new ArgumentException("Invalid ingredient ID", nameof(ingredient.IngredientId));

                if (ingredient.Quantity <= 0)
                    throw new ArgumentException("Ingredient quantity must be greater than 0", nameof(ingredient.Quantity));

                if (string.IsNullOrWhiteSpace(ingredient.Unit))
                    throw new ArgumentException("Ingredient unit is required", nameof(ingredient.Unit));
            }
        }

        /// <summary>
        /// Calculate total calories from recipe ingredients
        /// </summary>
        private decimal CalculateTotalCalories(List<RecipeIngredient> recipeIngredients)
        {
            decimal totalCalories = 0;

            foreach (var recipeIngredient in recipeIngredients)
            {
                var ingredientLookup = _ingredientService.GetIngredientForLookup(recipeIngredient.IngredientId);
                totalCalories += ingredientLookup.Calories * recipeIngredient.Quantity;
            }

            return totalCalories;
        }

        /// <summary>
        /// Map domain model to response DTO
        /// Enriches recipe ingredients with ingredient details from Ingredient Service
        /// </summary>
        private RecipeResponse MapToResponse(Recipe recipe)
        {
            var enrichedIngredients = new List<RecipeIngredientResponse>();

            foreach (var recipeIngredient in recipe.RecipeIngredients)
            {
                var ingredientLookup = _ingredientService.GetIngredientForLookup(recipeIngredient.IngredientId);

                enrichedIngredients.Add(new RecipeIngredientResponse
                {
                    IngredientId = recipeIngredient.IngredientId,
                    IngredientName = ingredientLookup.Name,
                    FoodGroup = ingredientLookup.FoodGroup,
                    Quantity = recipeIngredient.Quantity,
                    Unit = recipeIngredient.Unit,
                    CaloriesPerUnit = ingredientLookup.Calories
                });
            }

            return new RecipeResponse
            {
                Id = recipe.Id,
                Name = recipe.Name,
                Author = recipe.Author,
                TotalTimeMinutes = recipe.TotalTimeMinutes,
                CookTimeMinutes = recipe.CookTimeMinutes,
                Description = recipe.Description,
                TotalCalories = recipe.TotalCalories,
                Ingredients = enrichedIngredients,
                CreatedAt = recipe.CreatedAt
            };
        }
    }
}
