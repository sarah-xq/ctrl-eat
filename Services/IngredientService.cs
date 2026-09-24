using System;
using System.Collections.Generic;
using System.Linq;
using CtrlEat.DTOs;
using CtrlEat.Models;
using CtrlEat.Repositories.Interfaces;

namespace CtrlEat.Services
{
    /// <summary>
    /// Business logic service for Ingredient management
    /// Handles validation, transformation, and orchestration of ingredient operations
    /// Separates business logic from data access
    /// </summary>
    public class IngredientService
    {
        private readonly IIngredientRepository _repository;

        public IngredientService(IIngredientRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// Create a new ingredient
        /// </summary>
        public IngredientResponse CreateIngredient(CreateIngredientRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            ValidateIngredientRequest(request);

            var ingredient = new Ingredient
            {
                Name = request.Name.Trim(),
                FoodGroup = request.FoodGroup.Trim(),
                ExpiryDate = request.ExpiryDate,
                Calories = request.Calories
            };

            int id = _repository.Create(ingredient);
            ingredient.Id = id;

            return MapToResponse(ingredient);
        }

        /// <summary>
        /// Get ingredient by ID
        /// </summary>
        public IngredientResponse GetIngredient(int id)
        {
            var ingredient = _repository.GetById(id);
            if (ingredient == null)
                throw new InvalidOperationException($"Ingredient with id {id} not found");

            return MapToResponse(ingredient);
        }

        /// <summary>
        /// Search for ingredients
        /// </summary>
        public List<IngredientResponse> SearchIngredients(GetIngredientsRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            List<Ingredient> results = new List<Ingredient>();

            // Search by ID if provided
            if (request.Id.HasValue)
            {
                var ingredient = _repository.GetById(request.Id.Value);
                if (ingredient != null)
                    results.Add(ingredient);
            }
            // Search by name if provided
            else if (!string.IsNullOrWhiteSpace(request.Name))
            {
                results = _repository.GetByName(request.Name);
            }
            // Search by food group if provided
            else if (!string.IsNullOrWhiteSpace(request.FoodGroup))
            {
                results = _repository.GetByFoodGroup(request.FoodGroup);
            }
            // Get all if no criteria provided
            else
            {
                results = _repository.GetAll();
            }

            return results.Select(MapToResponse).ToList();
        }

        /// <summary>
        /// Update an existing ingredient
        /// </summary>
        public IngredientResponse UpdateIngredient(int id, CreateIngredientRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            ValidateIngredientRequest(request);

            var ingredient = _repository.GetById(id);
            if (ingredient == null)
                throw new InvalidOperationException($"Ingredient with id {id} not found");

            ingredient.Name = request.Name.Trim();
            ingredient.FoodGroup = request.FoodGroup.Trim();
            ingredient.ExpiryDate = request.ExpiryDate;
            ingredient.Calories = request.Calories;

            _repository.Update(ingredient);

            return MapToResponse(ingredient);
        }

        /// <summary>
        /// Delete an ingredient
        /// </summary>
        public void DeleteIngredient(int id)
        {
            var ingredient = _repository.GetById(id);
            if (ingredient == null)
                throw new InvalidOperationException($"Ingredient with id {id} not found");

            _repository.Delete(id);
        }

        /// <summary>
        /// Get ingredient details for inter-service communication
        /// Used by Recipe Service to enrich recipe ingredient information
        /// </summary>
        public IngredientLookupResponse GetIngredientForLookup(int id)
        {
            var ingredient = _repository.GetById(id);
            if (ingredient == null)
                throw new InvalidOperationException($"Ingredient with id {id} not found");

            return new IngredientLookupResponse
            {
                Id = ingredient.Id,
                Name = ingredient.Name,
                FoodGroup = ingredient.FoodGroup,
                Calories = ingredient.Calories,
                ExpiryDate = ingredient.ExpiryDate
            };
        }

        /// <summary>
        /// Validate ingredient request data
        /// </summary>
        private void ValidateIngredientRequest(CreateIngredientRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Ingredient name is required", nameof(request.Name));

            if (string.IsNullOrWhiteSpace(request.FoodGroup))
                throw new ArgumentException("Food group is required", nameof(request.FoodGroup));

            if (request.Calories < 0)
                throw new ArgumentException("Calories cannot be negative", nameof(request.Calories));

            if (request.ExpiryDate.HasValue && request.ExpiryDate.Value < DateTime.UtcNow)
                throw new ArgumentException("Expiry date cannot be in the past", nameof(request.ExpiryDate));
        }

        /// <summary>
        /// Map domain model to response DTO
        /// </summary>
        private IngredientResponse MapToResponse(Ingredient ingredient)
        {
            return new IngredientResponse
            {
                Id = ingredient.Id,
                Name = ingredient.Name,
                FoodGroup = ingredient.FoodGroup,
                ExpiryDate = ingredient.ExpiryDate,
                Calories = ingredient.Calories,
                CreatedAt = ingredient.CreatedAt
            };
        }
    }
}
