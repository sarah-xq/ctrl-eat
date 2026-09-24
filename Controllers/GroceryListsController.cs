using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using CtrlEat.DTOs;
using CtrlEat.Services;

namespace CtrlEat.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class GroceryListsController : ControllerBase
    {
        private readonly GroceryListService _service;

        public GroceryListsController(GroceryListService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<GroceryListResponse>), 200)]
        public ActionResult<List<GroceryListResponse>> GetGroceryLists([FromQuery] DateTime? date = null)
        {
            var results = _service.SearchGroceryLists(new GetGroceryListsRequest { Date = date });
            return Ok(results);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(GroceryListResponse), 200)]
        [ProducesResponseType(404)]
        public ActionResult<GroceryListResponse> GetGroceryList([FromRoute] int id)
        {
            try
            {
                return Ok(_service.GetGroceryList(id));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(GroceryListResponse), 201)]
        [ProducesResponseType(400)]
        public ActionResult<GroceryListResponse> CreateGroceryList([FromBody] CreateGroceryListRequest request)
        {
            try
            {
                var groceryList = _service.CreateGroceryList(request);
                return CreatedAtAction(nameof(GetGroceryList), new { id = groceryList.Id }, groceryList);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(GroceryListResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public ActionResult<GroceryListResponse> UpdateGroceryList(
            [FromRoute] int id,
            [FromBody] CreateGroceryListRequest request)
        {
            try
            {
                return Ok(_service.UpdateGroceryList(id, request));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public IActionResult DeleteGroceryList([FromRoute] int id)
        {
            try
            {
                _service.DeleteGroceryList(id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }
    }
}
