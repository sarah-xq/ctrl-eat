using User.Data;
using User.Domain.Entities;

namespace User.Api.Services;

public sealed class UserProfileService(UserProfileRepository repository)
{
    public async Task<int?> ReadGoalAsync(string issuer, string subject, CancellationToken ct)
    {
        var profile = await repository.ReadGoalAsync(issuer, subject, ct);
        return profile?.CalorieGoal;
    }

    public async Task<int> SaveGoalAsync(
        string issuer,
        string subject,
        int value,
        CancellationToken ct)
    {
        var profile = await repository.ReadGoalAsync(issuer, subject, ct)
            ?? new UserProfile(issuer, subject);
        profile.SetCalorieGoal(value);
        await repository.SaveGoalAsync(profile, ct);
        return profile.CalorieGoal!.Value;
    }
}