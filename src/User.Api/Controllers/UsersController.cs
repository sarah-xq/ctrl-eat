using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using User.Api.Services;

namespace User.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(UserProfileService service) : ControllerBase
{
    [HttpGet("me")]
    [Authorize(Policy = "users.read")]
    public async Task<ActionResult<CalorieGoalResponse>> GetMe(CancellationToken ct)
    {
        var identity = GetIdentity();
        var goal = await service.ReadGoalAsync(identity.Issuer, identity.Subject, ct);
        return Ok(new CalorieGoalResponse(goal));
    }

    [HttpPut("me/calorie-goal")]
    [Authorize(Policy = "users.write")]
    public async Task<ActionResult<CalorieGoalResponse>> SetCalorieGoal(
        [FromBody] CalorieGoalRequest? request,
        CancellationToken ct)
    {
        if (request?.CalorieGoal is not > 0)
        {
            return BadRequest(new { error = "calorieGoal must be a positive integer." });
        }

        var identity = GetIdentity();
        var goal = await service.SaveGoalAsync(
            identity.Issuer,
            identity.Subject,
            request.CalorieGoal.Value,
            ct);
        return Ok(new CalorieGoalResponse(goal));
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

public sealed record CalorieGoalRequest(int? CalorieGoal);

public sealed record CalorieGoalResponse(int? CalorieGoal);