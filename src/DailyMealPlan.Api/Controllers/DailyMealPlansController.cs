using System.Security.Claims;
using DailyMealPlan.Api.DTOs;
using DailyMealPlan.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DailyMealPlan.Api.Controllers;

[ApiController]
[Route("api/daily-meal-plans")]
public sealed class DailyMealPlansController(DailyMealPlanService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "daily-meal-plans.read")]
    public async Task<ActionResult<IReadOnlyList<DailyMealPlanResponse>>> List(
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var identity = GetIdentity();
        var plans = await service.ListAsync(identity.Issuer, identity.Subject, date, cancellationToken);
        return Ok(plans);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "daily-meal-plans.read")]
    public async Task<ActionResult<DailyMealPlanResponse>> Get(
        int id,
        CancellationToken cancellationToken)
    {
        var identity = GetIdentity();
        var plan = await service.GetAsync(id, identity.Issuer, identity.Subject, cancellationToken);
        return plan is null ? NotFound() : Ok(plan);
    }

    [HttpPost]
    [Authorize(Policy = "daily-meal-plans.write")]
    public async Task<ActionResult<DailyMealPlanResponse>> Create(
        [FromBody] DailyMealPlanRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { error = "A meal plan body is required." });
        }

        var identity = GetIdentity();
        try
        {
            var plan = await service.CreateAsync(
                request,
                identity.Issuer,
                identity.Subject,
                cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = plan.Id }, plan);
        }
        catch (DailyMealPlanValidationException exception)
        {
            return BadRequest(new { error = exception.Message, field = exception.Field });
        }
        catch (DailyMealPlanLookupUnavailableException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "A recipe or ingredient lookup service is unavailable." });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "daily-meal-plans.write")]
    public async Task<ActionResult<DailyMealPlanResponse>> Update(
        int id,
        [FromBody] DailyMealPlanRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { error = "A meal plan body is required." });
        }

        var identity = GetIdentity();
        try
        {
            var plan = await service.UpdateAsync(
                id,
                request,
                identity.Issuer,
                identity.Subject,
                cancellationToken);
            return plan is null ? NotFound() : Ok(plan);
        }
        catch (DailyMealPlanValidationException exception)
        {
            return BadRequest(new { error = exception.Message, field = exception.Field });
        }
        catch (DailyMealPlanLookupUnavailableException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "A recipe or ingredient lookup service is unavailable." });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "daily-meal-plans.write")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var identity = GetIdentity();
        var deleted = await service.DeleteAsync(id, identity.Issuer, identity.Subject, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private (string Issuer, string Subject) GetIdentity()
    {
        var issuer = User.FindFirst("iss")?.Value;
        var subject = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subject))
        {
            throw new InvalidOperationException("The validated token must contain iss and sub claims.");
        }

        return (issuer, subject);
    }
}