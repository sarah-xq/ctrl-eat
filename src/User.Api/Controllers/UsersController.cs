using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using User.Api.Security;
using User.Api.Services;
using User.Domain.Entities;

namespace User.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(
    UserProfileService service,
    UserProfilePictureService profilePictures) : ControllerBase
{
    [HttpGet("me")]
    [Authorize(Policy = SecurityPolicies.UsersRead)]
    public async Task<ActionResult<CalorieGoalResponse>> GetMe(CancellationToken ct)
    {
        var identity = GetIdentity();
        var goal = await service.ReadGoalAsync(identity.Issuer, identity.Subject, ct);
        return Ok(new CalorieGoalResponse(goal));
    }

    [HttpPut("me/calorie-goal")]
    [Authorize(Policy = SecurityPolicies.UsersWrite)]
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

    [HttpPut("me/profile-picture")]
    [Authorize(Policy = SecurityPolicies.UsersWrite)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 5 * 1024 * 1024)]
    public async Task<ActionResult<ProfilePictureResponse>> ReplaceProfilePicture(
        IFormFile? file,
        CancellationToken ct)
    {
        if (file is null || file.Length is 0)
        {
            return BadRequest(new { error = "A profile-picture file is required." });
        }

        if (file.Length > ProfilePictureRules.MaximumSizeBytes)
        {
            return BadRequest(new { error = "The profile picture exceeds the 5 MiB limit." });
        }

        await using var content = file.OpenReadStream();
        var bytes = new byte[file.Length];
        await content.ReadExactlyAsync(bytes, ct);
        var identity = GetIdentity();

        try
        {
            var picture = await profilePictures.ReplaceAsync(
                identity.Issuer,
                identity.Subject,
                new ProfilePictureUpload(file.ContentType, bytes),
                ct);
            return Ok(new ProfilePictureResponse(picture.Format.ToString(), picture.SizeBytes));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (ProfilePictureStorageException)
        {
            return StatusCode(StatusCodes.Status502BadGateway,
                new { error = "Profile picture storage is unavailable." });
        }
    }

    [HttpGet("me/profile-picture")]
    [Authorize(Policy = SecurityPolicies.UsersRead)]
    public async Task<IActionResult> GetProfilePicture(CancellationToken ct)
    {
        var identity = GetIdentity();
        var reference = await profilePictures.ReadAsync(
            identity.Issuer,
            identity.Subject,
            ct);
        if (reference is null)
        {
            return NotFound(new { error = "No profile picture is saved." });
        }

        Stream? stream;
        try
        {
            stream = await profilePictures.OpenReadAsync(reference, ct);
        }
        catch
        {
            return StatusCode(StatusCodes.Status502BadGateway,
                new { error = "Profile picture storage is unavailable." });
        }

        if (stream is null)
        {
            return NotFound(new { error = "No profile picture is saved." });
        }

        var contentType = reference.Format switch
        {
            ProfilePictureFormat.Jpeg => "image/jpeg",
            ProfilePictureFormat.Png => "image/png",
            ProfilePictureFormat.WebP => "image/webp",
            _ => throw new InvalidOperationException("Unsupported profile-picture format.")
        };
        return File(stream, contentType);
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

public sealed record ProfilePictureResponse(string Format, long SizeBytes);