using System;
using System.Collections.Generic;
using System.Linq;
using CtrlEat.DTOs;
using CtrlEat.Models;
using CtrlEat.Repositories.Interfaces;

namespace CtrlEat.Services
{
    /// <summary>
    /// Business logic for storage locations.
    /// Data access is delegated to IStorageLocationRepository.
    /// </summary>
    public class StorageLocationService
    {
        private readonly IStorageLocationRepository _repository;
        private readonly IngredientService _ingredientService;

        public StorageLocationService(
            IStorageLocationRepository repository,
            IngredientService ingredientService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _ingredientService = ingredientService ?? throw new ArgumentNullException(nameof(ingredientService));
        }

        public StorageLocationResponse CreateStorageLocation(CreateStorageLocationRequest request)
        {
            ValidateRequest(request);
            var ingredientIds = ValidateAndCleanIngredientIds(request.IngredientIds);

            var location = new StorageLocation
            {
                Name = request.Name.Trim(),
                Type = request.Type.Trim(),
                MinimumTemperature = request.MinimumTemperature,
                MaximumTemperature = request.MaximumTemperature,
                IngredientIds = ingredientIds
            };

            location.Id = _repository.Create(location);
            return MapToResponse(location);
        }

        public StorageLocationResponse GetStorageLocation(int id)
        {
            var location = _repository.GetById(id);
            if (location == null)
                throw new InvalidOperationException($"Storage location with id {id} not found");

            return MapToResponse(location);
        }

        public List<StorageLocationResponse> SearchStorageLocations(GetStorageLocationsRequest request)
        {
            request ??= new GetStorageLocationsRequest();
            List<StorageLocation> results;

            if (request.Id.HasValue)
            {
                var location = _repository.GetById(request.Id.Value);
                results = location == null
                    ? new List<StorageLocation>()
                    : new List<StorageLocation> { location };
            }
            else if (!string.IsNullOrWhiteSpace(request.Name))
            {
                results = _repository.GetByName(request.Name);
            }
            else if (!string.IsNullOrWhiteSpace(request.Type))
            {
                results = _repository.GetByType(request.Type);
            }
            else
            {
                results = _repository.GetAll();
            }

            return results.Select(MapToResponse).ToList();
        }

        public StorageLocationResponse UpdateStorageLocation(int id, CreateStorageLocationRequest request)
        {
            ValidateRequest(request);
            var location = _repository.GetById(id);
            if (location == null)
                throw new InvalidOperationException($"Storage location with id {id} not found");

            location.Name = request.Name.Trim();
            location.Type = request.Type.Trim();
            location.MinimumTemperature = request.MinimumTemperature;
            location.MaximumTemperature = request.MaximumTemperature;
            location.IngredientIds = ValidateAndCleanIngredientIds(request.IngredientIds);

            _repository.Update(location);
            return MapToResponse(location);
        }

        public void DeleteStorageLocation(int id)
        {
            _repository.Delete(id);
        }

        private void ValidateRequest(CreateStorageLocationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Storage location name is required", nameof(request.Name));
            if (string.IsNullOrWhiteSpace(request.Type))
                throw new ArgumentException("Storage location type is required", nameof(request.Type));
            if (request.MinimumTemperature > request.MaximumTemperature)
                throw new ArgumentException("Minimum temperature cannot be greater than maximum temperature");
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

        private StorageLocationResponse MapToResponse(StorageLocation location)
        {
            var response = new StorageLocationResponse
            {
                Id = location.Id,
                Name = location.Name,
                Type = location.Type,
                MinimumTemperature = location.MinimumTemperature,
                MaximumTemperature = location.MaximumTemperature,
                CreatedAt = location.CreatedAt
            };

            foreach (var ingredientId in location.IngredientIds)
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
