using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StorageLocation.Data.Entities;

namespace StorageLocation.Data.Configurations;

public sealed class StorageLocationIngredientRecordConfiguration : IEntityTypeConfiguration<StorageLocationIngredientRecord>
{
    public void Configure(EntityTypeBuilder<StorageLocationIngredientRecord> builder)
    {
        builder.ToTable("StorageLocationIngredients", table =>
            table.HasCheckConstraint("CK_StorageLocationIngredients_IngredientId_Positive", "\"IngredientId\" > 0"));
        builder.HasKey(ingredient => new { ingredient.StorageLocationId, ingredient.IngredientId });
        builder.HasIndex(ingredient => new { ingredient.StorageLocationId, ingredient.Position });

        builder.Property(ingredient => ingredient.StorageLocationId)
            .HasColumnType("INTEGER");
        builder.Property(ingredient => ingredient.IngredientId)
            .HasColumnType("INTEGER")
            .IsRequired();
        builder.Property(ingredient => ingredient.Position)
            .HasColumnType("INTEGER")
            .IsRequired();
    }
}
