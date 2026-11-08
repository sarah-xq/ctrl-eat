using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace StorageLocation.Api.Clients;

public sealed class HttpIngredientClient(HttpClient httpClient) : IIngredientClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IngredientLookupResult?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync($"api/Ingredients/{id}", cancellationToken);
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

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new IngredientNotFoundException(id);
            }

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw new IngredientUpstreamAuthorizationException((int)response.StatusCode);
            }

            if ((int)response.StatusCode >= 500)
            {
                throw new IngredientLookupUnavailableException(
                    new HttpRequestException($"Ingredient service returned HTTP {(int)response.StatusCode}."));
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Ingredient service returned unexpected HTTP {(int)response.StatusCode}.");
            }

            IngredientResponse? ingredient;
            try
            {
                ingredient = await response.Content.ReadFromJsonAsync<IngredientResponse>(JsonOptions, cancellationToken);
            }
            catch (IOException exception)
            {
                throw new IngredientLookupUnavailableException(exception);
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                throw new IngredientLookupUnavailableException(exception);
            }

            if (ingredient is null || ingredient.Id != id)
            {
                throw new InvalidOperationException("Ingredient service returned an empty or mismatched ingredient response.");
            }

            return new IngredientLookupResult(ingredient.Id, ingredient.Name, ingredient.FoodGroup, ingredient.Calories);
        }
    }

    private sealed class IngredientResponse
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string FoodGroup { get; init; } = string.Empty;
        public decimal Calories { get; init; }
    }
}