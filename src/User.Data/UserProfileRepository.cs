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
}