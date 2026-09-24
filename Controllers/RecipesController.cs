using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using CtrlEat.DTOs;
using CtrlEat.Services;

namespace CtrlEat.Controllers
{
    /// <summary>
    /// HTTP REST API Controller for Recipe Service
    /// Provides CRUD and search endpoints for recipes
    /// Handles recipe-ingredient management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class RecipesController : ControllerBase
    {
        private readonly RecipeService _recipeService;

        public RecipesController(RecipeService recipeService)
        {
            _recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
        }

        /// <summary>
        /// Get all recipes or search by criteria
        /// </summary>
        /// <param name="name">Optional: Search by recipe name</param>
        /// <param name="author">Optional: Search by recipe author</param>
        /// <returns>List of recipes matching criteria</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<RecipeResponse>), 200)]
        public ActionResult<List<RecipeResponse>> GetRecipes(
            [FromQuery] string name = null,
            [FromQuery] string author = null)
        {
            try
            {
                var request = new GetRecipesRequest
                {
                    Name = name,
                    Author = author
                };

                var results = _recipeService.SearchRecipes(request);
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
        /// Get a specific recipe by ID
        /// </summary>
        /// <param name="id">Recipe ID</param>
        /// <returns>Recipe details with all ingredients</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RecipeResponse), 200)]
        [ProducesResponseType(404)]
        public ActionResult<RecipeResponse> GetRecipe([FromRoute] int id)
        {
            try
            {
                var recipe = _recipeService.GetRecipe(id);
                return Ok(recipe);
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
        /// Create a new recipe
        /// </summary>
        /// <param name="request">Recipe creation request with ingredients</param>
        /// <returns>Created recipe</returns>
        [HttpPost]
        [ProducesResponseType(typeof(RecipeResponse), 201)]
        [ProducesResponseType(400)]
        public ActionResult<RecipeResponse> CreateRecipe([FromBody] CreateRecipeRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { error = "Request body cannot be empty" });
                }

                var recipe = _recipeService.CreateRecipe(request);
                return CreatedAtAction(nameof(GetRecipe), new { id = recipe.Id }, recipe);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing recipe
        /// </summary>
        /// <param name="id">Recipe ID</param>
        /// <param name="request">Updated recipe data</param>
        /// <returns>Updated recipe</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(RecipeResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public ActionResult<RecipeResponse> UpdateRecipe(
            [FromRoute] int id,
            [FromBody] CreateRecipeRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { error = "Request body cannot be empty" });
                }

                var recipe = _recipeService.UpdateRecipe(id, request);
                return Ok(recipe);
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
        /// Delete a recipe
        /// </summary>
        /// <param name="id">Recipe ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public IActionResult DeleteRecipe([FromRoute] int id)
        {
            try
            {
                _recipeService.DeleteRecipe(id);
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

        /// <summary>
        /// Add an ingredient to an existing recipe
        /// </summary>
        /// <param name="recipeId">Recipe ID</param>
        /// <param name="request">Ingredient to add with quantity</param>
        /// <returns>Updated recipe</returns>
        [HttpPost("{recipeId}/ingredients")]
        [ProducesResponseType(typeof(RecipeResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public ActionResult<RecipeResponse> AddIngredientToRecipe(
            [FromRoute] int recipeId,
            [FromBody] RecipeIngredientRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { error = "Request body cannot be empty" });
                }

                var recipe = _recipeService.AddIngredientToRecipe(recipeId, request);
                return Ok(recipe);
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
        /// Remove an ingredient from a recipe
        /// </summary>
        /// <param name="recipeId">Recipe ID</param>
        /// <param name="ingredientId">Ingredient ID to remove</param>
        /// <returns>Updated recipe</returns>
        [HttpDelete("{recipeId}/ingredients/{ingredientId}")]
        [ProducesResponseType(typeof(RecipeResponse), 200)]
        [ProducesResponseType(404)]
        public ActionResult<RecipeResponse> RemoveIngredientFromRecipe(
            [FromRoute] int recipeId,
            [FromRoute] int ingredientId)
        {
            try
            {
                var recipe = _recipeService.RemoveIngredientFromRecipe(recipeId, ingredientId);
                return Ok(recipe);
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
