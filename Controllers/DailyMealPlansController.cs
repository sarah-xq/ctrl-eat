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
    public class DailyMealPlansController : ControllerBase
    {
        private readonly DailyMealPlanService _service;

        public DailyMealPlansController(DailyMealPlanService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<DailyMealPlanResponse>), 200)]
        public ActionResult<List<DailyMealPlanResponse>> GetDailyMealPlans([FromQuery] int? userId = null, [FromQuery] DateTime? date = null)
        {
            try
            {
                var request = new GetDailyMealPlansRequest { UserId = userId, Date = date };
                var results = _service.SearchDailyMealPlans(request);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(DailyMealPlanResponse), 200)]
        [ProducesResponseType(404)]
        public ActionResult<DailyMealPlanResponse> GetDailyMealPlan([FromRoute] int id)
        {
            try
            {
                var plan = _service.GetDailyMealPlan(id);
                return Ok(plan);
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

        [HttpPost]
        [ProducesResponseType(typeof(DailyMealPlanResponse), 201)]
        [ProducesResponseType(400)]
        public ActionResult<DailyMealPlanResponse> CreateDailyMealPlan([FromBody] CreateDailyMealPlanRequest request)
        {
            try
            {
                var plan = _service.CreateDailyMealPlan(request);
                return CreatedAtAction(nameof(GetDailyMealPlan), new { id = plan.Id }, plan);
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

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(DailyMealPlanResponse), 200)]
        [ProducesResponseType(404)]
        public ActionResult<DailyMealPlanResponse> UpdateDailyMealPlan([FromRoute] int id, [FromBody] UpdateDailyMealPlanRequest request)
        {
            try
            {
                var plan = _service.UpdateDailyMealPlan(id, request);
                return Ok(plan);
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

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public IActionResult DeleteDailyMealPlan([FromRoute] int id)
        {
            try
            {
                _service.DeleteDailyMealPlan(id);
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
