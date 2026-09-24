using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using CtrlEat.DTOs;
using CtrlEat.Services;

namespace CtrlEat.Controllers
{
    /// <summary>
    /// HTTP REST API Controller for Ingredient Service
    /// Provides CRUD and search endpoints for ingredients
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class IngredientsController : ControllerBase
    {
        private readonly IngredientService _ingredientService;

        public IngredientsController(IngredientService ingredientService)
        {
            _ingredientService = ingredientService ?? throw new ArgumentNullException(nameof(ingredientService));
        }

        /// <summary>
        /// Get all ingredients or search by criteria
        /// </summary>
        /// <param name="name">Optional: Search by ingredient name</param>
        /// <param name="foodGroup">Optional: Search by food group</param>
        /// <returns>List of ingredients matching criteria</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<IngredientResponse>), 200)]
        public ActionResult<List<IngredientResponse>> GetIngredients(
            [FromQuery] string name = null,
            [FromQuery] string foodGroup = null)
        {
            try
            {
                var request = new GetIngredientsRequest
                {
                    Name = name,
                    FoodGroup = foodGroup
                };

                var results = _ingredientService.SearchIngredients(request);
                return Ok(results);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific ingredient by ID
        /// </summary>
        /// <param name="id">Ingredient ID</param>
        /// <returns>Ingredient details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(IngredientResponse), 200)]
        [ProducesResponseType(404)]
        public ActionResult<IngredientResponse> GetIngredient([FromRoute] int id)
        {
            try
            {
                var ingredient = _ingredientService.GetIngredient(id);
                return Ok(ingredient);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Create a new ingredient
        /// </summary>
        /// <param name="request">Ingredient creation request</param>
        /// <returns>Created ingredient</returns>
        [HttpPost]
        [ProducesResponseType(typeof(IngredientResponse), 201)]
        [ProducesResponseType(400)]
        public ActionResult<IngredientResponse> CreateIngredient([FromBody] CreateIngredientRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { error = "Request body cannot be empty" });
                }

                var ingredient = _ingredientService.CreateIngredient(request);
                return CreatedAtAction(nameof(GetIngredient), new { id = ingredient.Id }, ingredient);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing ingredient
        /// </summary>
        /// <param name="id">Ingredient ID</param>
        /// <param name="request">Updated ingredient data</param>
        /// <returns>Updated ingredient</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(IngredientResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public ActionResult<IngredientResponse> UpdateIngredient(
            [FromRoute] int id,
            [FromBody] CreateIngredientRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { error = "Request body cannot be empty" });
                }

                var ingredient = _ingredientService.UpdateIngredient(id, request);
                return Ok(ingredient);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete an ingredient
        /// </summary>
        /// <param name="id">Ingredient ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public IActionResult DeleteIngredient([FromRoute] int id)
        {
            try
            {
                _ingredientService.DeleteIngredient(id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }
    }
}
