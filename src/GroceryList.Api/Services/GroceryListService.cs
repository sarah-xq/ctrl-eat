using GroceryList.Api.Clients;
using GroceryList.Api.DTOs;
using GroceryList.Data.Repositories;
using DomainGroceryList = GroceryList.Domain.Entities.GroceryList;

namespace GroceryList.Api.Services;

public sealed class GroceryListService(
    IGroceryListRepository repository,
    IIngredientClient ingredientClient)
{
    public async Task<IReadOnlyList<GroceryListResponse>> GetAllAsync(
        DateTime? date,
        CancellationToken cancellationToken)
    {
        var lists = await repository.GetAllAsync(date?.Date, cancellationToken);
        var responses = new List<GroceryListResponse>(lists.Count);
        foreach (var list in lists)
        {
            responses.Add(await ToResponseAsync(list, cancellationToken));
        }

        return responses;
    }

    public async Task<GroceryListResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var list = await repository.GetByIdAsync(id, cancellationToken);
        return list is null ? null : await ToResponseAsync(list, cancellationToken);
    }

    public async Task<GroceryListResponse> CreateAsync(
        GroceryListRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var list = new DomainGroceryList(request.Date, request.IngredientIds);
        var ingredients = await ResolveIngredientsAsync(list.IngredientIds, cancellationToken);
        await repository.CreateAsync(list, cancellationToken);
        return ToResponse(list, ingredients);
    }

    public async Task<GroceryListResponse?> UpdateAsync(
        int id,
        GroceryListRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var list = await repository.GetByIdAsync(id, cancellationToken);
        if (list is null)
        {
            return null;
        }

        list.Update(request.Date, request.IngredientIds);
        var ingredients = await ResolveIngredientsAsync(list.IngredientIds, cancellationToken);
        if (!await repository.UpdateAsync(list, cancellationToken))
        {
            return null;
        }

        return ToResponse(list, ingredients);
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

        var results = new List<IngredientLookupResult>(ingredientIds.Count);
        foreach (var ingredientId in ingredientIds)
        {
            IngredientLookupResult? ingredient;
            try
            {
                ingredient = await ingredientClient.GetByIdAsync(ingredientId, cancellationToken);
            }
            catch (IngredientUpstreamAuthorizationException)
            {
                throw;
            }
            catch (IngredientLookupUnavailableException)
            {
                throw;
            }
            catch (HttpRequestException exception)
            {
                throw new IngredientLookupUnavailableException(exception);
            }
            catch (IOException exception)
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

    private async Task<GroceryListResponse> ToResponseAsync(
        DomainGroceryList list,
        CancellationToken cancellationToken)
    {
        var ingredients = await ResolveIngredientsAsync(list.IngredientIds, cancellationToken);
        return ToResponse(list, ingredients);
    }

    private static GroceryListResponse ToResponse(
        DomainGroceryList list,
        IReadOnlyList<IngredientLookupResult> ingredients) => new(
            list.Id,
            ingredients.Select(ingredient => new StoredIngredientResponse(
                ingredient.Id,
                ingredient.Name,
                ingredient.FoodGroup,
                ingredient.Calories)).ToList(),
            list.Date,
            list.CreatedAt);
}
