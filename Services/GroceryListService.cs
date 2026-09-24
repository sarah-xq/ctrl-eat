using System;
using System.Collections.Generic;
using System.Linq;
using CtrlEat.DTOs;
using CtrlEat.Models;
using CtrlEat.Repositories.Interfaces;

namespace CtrlEat.Services
{
    /// <summary>
    /// Business logic for grocery lists.
    /// Data access is delegated to IGroceryListRepository.
    /// </summary>
    public class GroceryListService
    {
        private readonly IGroceryListRepository _repository;
        private readonly IngredientService _ingredientService;

        public GroceryListService(
            IGroceryListRepository repository,
            IngredientService ingredientService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _ingredientService = ingredientService ?? throw new ArgumentNullException(nameof(ingredientService));
        }

        public GroceryListResponse CreateGroceryList(CreateGroceryListRequest request)
        {
            ValidateRequest(request);

            var groceryList = new GroceryList
            {
                IngredientIds = ValidateAndCleanIngredientIds(request.IngredientIds),
                Date = request.Date.Date
            };

            groceryList.Id = _repository.Create(groceryList);
            return MapToResponse(groceryList);
        }

        public GroceryListResponse GetGroceryList(int id)
        {
            var groceryList = _repository.GetById(id);
            if (groceryList == null)
                throw new InvalidOperationException($"Grocery list with id {id} not found");

            return MapToResponse(groceryList);
        }

        public List<GroceryListResponse> SearchGroceryLists(GetGroceryListsRequest request)
        {
            request ??= new GetGroceryListsRequest();
            List<GroceryList> results;

            if (request.Id.HasValue)
            {
                var groceryList = _repository.GetById(request.Id.Value);
                results = groceryList == null
                    ? new List<GroceryList>()
                    : new List<GroceryList> { groceryList };
            }
            else if (request.Date.HasValue)
            {
                results = _repository.GetByDate(request.Date.Value);
            }
            else
            {
                results = _repository.GetAll();
            }

            return results.Select(MapToResponse).ToList();
        }

        public GroceryListResponse UpdateGroceryList(int id, CreateGroceryListRequest request)
        {
            ValidateRequest(request);
            var groceryList = _repository.GetById(id);
            if (groceryList == null)
                throw new InvalidOperationException($"Grocery list with id {id} not found");

            groceryList.IngredientIds = ValidateAndCleanIngredientIds(request.IngredientIds);
            groceryList.Date = request.Date.Date;

            _repository.Update(groceryList);
            return MapToResponse(groceryList);
        }

        public void DeleteGroceryList(int id)
        {
            _repository.Delete(id);
        }

        private void ValidateRequest(CreateGroceryListRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.Date == default)
                throw new ArgumentException("Grocery list date is required", nameof(request.Date));
        }

        private List<int> ValidateAndCleanIngredientIds(List<int> ingredientIds)
        {
            var cleanedIds = (ingredientIds ?? new List<int>()).Distinct().ToList();

            foreach (var ingredientId in cleanedIds)
            {
                if (ingredientId <= 0)
                    throw new ArgumentException("Ingredient IDs must be greater than zero");

                _ingredientService.GetIngredientForLookup(ingredientId);
            }

            return cleanedIds;
        }

        private GroceryListResponse MapToResponse(GroceryList groceryList)
        {
            var response = new GroceryListResponse
            {
                Id = groceryList.Id,
                Date = groceryList.Date,
                CreatedAt = groceryList.CreatedAt
            };

            foreach (var ingredientId in groceryList.IngredientIds)
            {
                var ingredient = _ingredientService.GetIngredientForLookup(ingredientId);
                response.Ingredients.Add(new StoredIngredientResponse
                {
                    Id = ingredient.Id,
                    Name = ingredient.Name,
                    FoodGroup = ingredient.FoodGroup,
                    Calories = ingredient.Calories
                });
            }

            return response;
        }
    }
}
