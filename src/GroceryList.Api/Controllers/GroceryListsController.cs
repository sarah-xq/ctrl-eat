using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GroceryList.Api.DTOs;
using GroceryList.Api.Services;

namespace GroceryList.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class GroceryListsController(GroceryListService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "grocery-lists.read")]
    [ProducesResponseType(typeof(IReadOnlyList<GroceryListResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GroceryListResponse>>> GetGroceryLists(
        [FromQuery] DateTime? date,
        CancellationToken cancellationToken) =>
        Ok(await service.GetAllAsync(date, cancellationToken));

    [HttpGet("{id:int}")]
    [Authorize(Policy = "grocery-lists.read")]
    [ProducesResponseType(typeof(GroceryListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroceryListResponse>> GetGroceryList(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var groceryList = await service.GetByIdAsync(id, cancellationToken);
        return groceryList is null
            ? NotFound(new { error = $"Grocery list with id {id} was not found." })
            : Ok(groceryList);
    }

    [HttpPost]
    [Authorize(Policy = "grocery-lists.write")]
    [ProducesResponseType(typeof(GroceryListResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GroceryListResponse>> CreateGroceryList(
        [FromBody] GroceryListRequest request,
        CancellationToken cancellationToken)
    {
        var groceryList = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetGroceryList), new { id = groceryList.Id }, groceryList);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "grocery-lists.write")]
    [ProducesResponseType(typeof(GroceryListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroceryListResponse>> UpdateGroceryList(
        [FromRoute] int id,
        [FromBody] GroceryListRequest request,
        CancellationToken cancellationToken)
    {
        var groceryList = await service.UpdateAsync(id, request, cancellationToken);
        return groceryList is null
            ? NotFound(new { error = $"Grocery list with id {id} was not found." })
            : Ok(groceryList);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "grocery-lists.write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGroceryList(
        [FromRoute] int id,
        CancellationToken cancellationToken) =>
        await service.DeleteAsync(id, cancellationToken)
            ? NoContent()
            : NotFound(new { error = $"Grocery list with id {id} was not found." });
}
