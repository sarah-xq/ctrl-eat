using Microsoft.EntityFrameworkCore;
using User.Domain.Entities;

namespace User.Data;

public sealed class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
{
    public DbSet<UserProfile> Profiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserProfileConfiguration());
    }
}