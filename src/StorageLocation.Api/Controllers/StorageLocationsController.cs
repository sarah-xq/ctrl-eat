using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StorageLocation.Api.Clients;
using StorageLocation.Api.DTOs;
using StorageLocation.Api.Services;

namespace StorageLocation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[AllowAnonymous]
public sealed class StorageLocationsController(StorageLocationService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StorageLocationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StorageLocationResponse>>> GetStorageLocations(
        [FromQuery] string? name,
        [FromQuery] string? type,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.SearchAsync(name, type, cancellationToken));
        }
        catch (IngredientLookupUnavailableException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = exception.Message });
        }
        catch (IngredientNotFoundException exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = exception.Message });
        }
        catch (IngredientUpstreamAuthorizationException exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = exception.Message });
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StorageLocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorageLocationResponse>> GetStorageLocation(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var location = await service.GetByIdAsync(id, cancellationToken);
            return location is null ? NotFound(new { error = $"Storage location with id {id} was not found." }) : Ok(location);
        }
        catch (IngredientLookupUnavailableException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = exception.Message });
        }
        catch (IngredientNotFoundException exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = exception.Message });
        }
        catch (IngredientUpstreamAuthorizationException exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = exception.Message });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(StorageLocationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StorageLocationResponse>> CreateStorageLocation(
        [FromBody] CreateStorageLocationRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { error = "A request body is required." });
        }

        try
        {
            var location = await service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetStorageLocation), new { id = location.Id }, location);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (IngredientNotFoundException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (IngredientLookupUnavailableException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = exception.Message });
        }
        catch (IngredientUpstreamAuthorizationException exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = exception.Message });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(StorageLocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorageLocationResponse>> UpdateStorageLocation(
        [FromRoute] int id,
        [FromBody] CreateStorageLocationRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { error = "A request body is required." });
        }

        try
        {
            var location = await service.UpdateAsync(id, request, cancellationToken);
            return location is null
                ? NotFound(new { error = $"Storage location with id {id} was not found." })
                : Ok(location);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (IngredientNotFoundException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (IngredientLookupUnavailableException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = exception.Message });
        }
        catch (IngredientUpstreamAuthorizationException exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = exception.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStorageLocation([FromRoute] int id, CancellationToken cancellationToken)
    {
        return await service.DeleteAsync(id, cancellationToken)
            ? NoContent()
            : NotFound(new { error = $"Storage location with id {id} was not found." });
    }
}
