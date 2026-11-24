using Microsoft.EntityFrameworkCore;
using User.Domain.Entities;

namespace User.Data;

public sealed class UserProfileRepository(UserDbContext dbContext)
{
    public Task<UserProfile?> ReadGoalAsync(
        string issuer,
        string subject,
        CancellationToken ct)
    {
        return dbContext.Profiles
            .SingleOrDefaultAsync(
                profile => profile.Issuer == issuer && profile.Subject == subject,
                ct);
    }

    public async Task SaveGoalAsync(UserProfile profile, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(profile);

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Profiles" ("Issuer", "Subject", "CalorieGoal")
            VALUES ({profile.Issuer}, {profile.Subject}, {profile.CalorieGoal})
            ON CONFLICT ("Issuer", "Subject") DO UPDATE SET
                "CalorieGoal" = excluded."CalorieGoal";
            """, ct);
    }

    public async Task SaveProfilePictureAsync(
        UserProfile profile,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var existing = await dbContext.Profiles.SingleOrDefaultAsync(
            item => item.Issuer == profile.Issuer && item.Subject == profile.Subject,
            ct);

        if (existing is null)
        {
            dbContext.Profiles.Add(profile);
        }
        else
        {
            existing.SetProfilePicture(profile.ProfilePicture!);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    public async Task ClearProfilePictureAsync(
        string issuer,
        string subject,
        CancellationToken ct)
    {
        var profile = await dbContext.Profiles.SingleOrDefaultAsync(
            item => item.Issuer == issuer && item.Subject == subject,
            ct);

        if (profile is null)
        {
            return;
        }

        profile.ClearProfilePicture();
        await dbContext.SaveChangesAsync(ct);
    }
}