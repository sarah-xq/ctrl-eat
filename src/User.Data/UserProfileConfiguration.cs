using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using User.Domain.Entities;

namespace User.Data;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("Profiles");
        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.Issuer)
            .HasColumnName("Issuer")
            .IsRequired();

        builder.Property(profile => profile.Subject)
            .HasColumnName("Subject")
            .IsRequired();

        builder.Property(profile => profile.CalorieGoal)
            .HasColumnName("CalorieGoal");

        builder.HasIndex(profile => new { profile.Issuer, profile.Subject })
            .IsUnique();
    }
}