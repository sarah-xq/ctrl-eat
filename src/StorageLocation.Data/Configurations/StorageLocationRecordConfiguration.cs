using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StorageLocation.Data.Entities;

namespace StorageLocation.Data.Configurations;

public sealed class StorageLocationRecordConfiguration : IEntityTypeConfiguration<StorageLocationRecord>
{
    private static readonly ValueConverter<DateTime, long> UtcTicksConverter = new(
        value => value.Ticks,
        ticks => new DateTime(ticks, DateTimeKind.Utc));

    public void Configure(EntityTypeBuilder<StorageLocationRecord> builder)
    {
        builder.ToTable("StorageLocations");
        builder.HasKey(location => location.Id);

        builder.Property(location => location.Id)
            .ValueGeneratedOnAdd();
        builder.Property(location => location.Name)
            .HasColumnType("TEXT")
            .IsRequired();
        builder.Property(location => location.Type)
            .HasColumnType("TEXT")
            .IsRequired();
        builder.Property(location => location.MinimumTemperature)
            .HasColumnType("TEXT")
            .IsRequired();
        builder.Property(location => location.MaximumTemperature)
            .HasColumnType("TEXT")
            .IsRequired();
        builder.Property(location => location.CreatedAt)
            .HasConversion(UtcTicksConverter)
            .HasColumnType("INTEGER")
            .IsRequired();
        builder.Property(location => location.UpdatedAt)
            .HasConversion(UtcTicksConverter)
            .HasColumnType("INTEGER")
            .IsRequired();

        builder.HasMany(location => location.IngredientReferences)
            .WithOne()
            .HasForeignKey(ingredient => ingredient.StorageLocationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
