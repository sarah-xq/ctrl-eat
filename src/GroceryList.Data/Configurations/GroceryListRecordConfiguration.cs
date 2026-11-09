using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using GroceryList.Data.Entities;

namespace GroceryList.Data.Configurations;

public sealed class GroceryListRecordConfiguration : IEntityTypeConfiguration<GroceryListRecord>
{
    private static readonly ValueConverter<DateTime, long> UtcTicksConverter = new(
        value => value.Ticks,
        ticks => new DateTime(ticks, DateTimeKind.Utc));

    public void Configure(EntityTypeBuilder<GroceryListRecord> builder)
    {
        builder.ToTable("GroceryLists");
        builder.HasKey(list => list.Id);

        builder.Property(list => list.Id).ValueGeneratedOnAdd();
        builder.Property(list => list.Date)
            .HasColumnType("TEXT")
            .IsRequired();
        builder.Property(list => list.CreatedAt)
            .HasConversion(UtcTicksConverter)
            .HasColumnType("INTEGER")
            .IsRequired();
        builder.Property(list => list.UpdatedAt)
            .HasConversion(UtcTicksConverter)
            .HasColumnType("INTEGER")
            .IsRequired();

        builder.HasMany(list => list.IngredientReferences)
            .WithOne()
            .HasForeignKey(ingredient => ingredient.GroceryListId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}