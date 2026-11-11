using StorageLocation.Api.Clients;
using StorageLocation.Api.DTOs;
using StorageLocation.Data.Repositories;
using DomainStorageLocation = StorageLocation.Domain.Entities.StorageLocation;

namespace StorageLocation.Api.Services;

public sealed class StorageLocationService(
    IStorageLocationRepository repository,
    IIngredientClient? ingredientClient = null)
{
    public async Task<IReadOnlyList<StorageLocationResponse>> SearchAsync(
        string? name,
        string? type,
        CancellationToken cancellationToken)
    {
        var locations = await repository.FindAsync(name, type, cancellationToken);
        var responses = new List<StorageLocationResponse>(locations.Count);
        foreach (var location in locations)
        {
            var ingredients = await ResolveIngredientsAsync(location.IngredientIds, cancellationToken);
            responses.Add(ToResponse(location, ingredients));
        }

        return responses;
    }

    public async Task<StorageLocationResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var location = await repository.GetByIdAsync(id, cancellationToken);
        if (location is null)
        {
            return null;
        }

        var ingredients = await ResolveIngredientsAsync(location.IngredientIds, cancellationToken);
        return ToResponse(location, ingredients);
    }

    public async Task<StorageLocationResponse> CreateAsync(
        CreateStorageLocationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var location = new DomainStorageLocation(
            request.Name,
            request.Type,
            request.MinimumTemperature,
            request.MaximumTemperature,
            request.IngredientIds);
        var ingredients = await ResolveIngredientsAsync(location.IngredientIds, cancellationToken);

        await repository.CreateAsync(location, cancellationToken);
        return ToResponse(location, ingredients);
    }

    public async Task<StorageLocationResponse?> UpdateAsync(
        int id,
        CreateStorageLocationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var location = await repository.GetByIdAsync(id, cancellationToken);
        if (location is null)
        {
            return null;
        }

        location.Update(
            request.Name,
            request.Type,
            request.MinimumTemperature,
            request.MaximumTemperature,
            request.IngredientIds);
        var ingredients = await ResolveIngredientsAsync(location.IngredientIds, cancellationToken);

        if (!await repository.UpdateAsync(location, cancellationToken))
        {
            return null;
        }

        return ToResponse(location, ingredients);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
        repository.DeleteAsync(id, cancellationToken);

    private async Task<IReadOnlyList<IngredientLookupResult>> ResolveIngredientsAsync(
        IReadOnlyList<int> ingredientIds,
        CancellationToken cancellationToken)
    {
        if (ingredientIds.Count == 0)
        {
            return [];
        }

        if (ingredientClient is null)
        {
            throw new IngredientLookupUnavailableException();
        }

        var results = new List<IngredientLookupResult>(ingredientIds.Count);
        foreach (var ingredientId in ingredientIds)
        {
            IngredientLookupResult? ingredient;
            try
            {
                ingredient = await ingredientClient.GetByIdAsync(ingredientId, cancellationToken);
            }
            catch (HttpRequestException exception)
            {
                throw new IngredientLookupUnavailableException(exception);
            }
            catch (IOException exception)
            {
                throw new IngredientLookupUnavailableException(exception);
            }
            catch (System.Net.Sockets.SocketException exception)
            {
                throw new IngredientLookupUnavailableException(exception);
            }
            catch (TimeoutException exception)
            {
                throw new IngredientLookupUnavailableException(exception);
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                throw new IngredientLookupUnavailableException(exception);
            }

            if (ingredient is null || ingredient.Id != ingredientId)
            {
                throw new IngredientNotFoundException(ingredientId);
            }

            results.Add(ingredient);
        }

        return results;
    }

    private static StorageLocationResponse ToResponse(
        DomainStorageLocation location,
        IReadOnlyList<IngredientLookupResult> ingredients) => new()
    {
        Id = location.Id,
        Name = location.Name,
        Type = location.Type,
        MinimumTemperature = location.MinimumTemperature,
        MaximumTemperature = location.MaximumTemperature,
        Ingredients = ingredients
            .Select(ingredient => new StoredIngredientResponse(
                ingredient.Id,
                ingredient.Name,
                ingredient.FoodGroup,
                ingredient.Calories))
            .ToList(),
        CreatedAt = location.CreatedAt
    };
}
