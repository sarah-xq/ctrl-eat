using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GroceryList.Data.Entities;

namespace GroceryList.Data.Configurations;

public sealed class GroceryListIngredientRecordConfiguration : IEntityTypeConfiguration<GroceryListIngredientRecord>
{
    public void Configure(EntityTypeBuilder<GroceryListIngredientRecord> builder)
    {
        builder.ToTable("GroceryListIngredients", table =>
            table.HasCheckConstraint("CK_GroceryListIngredients_IngredientId_Positive", "\"IngredientId\" > 0"));
        builder.HasKey(ingredient => new { ingredient.GroceryListId, ingredient.IngredientId });
        builder.HasIndex(ingredient => new { ingredient.GroceryListId, ingredient.Position });

        builder.Property(ingredient => ingredient.GroceryListId).HasColumnType("INTEGER");
        builder.Property(ingredient => ingredient.IngredientId)
            .HasColumnType("INTEGER")
            .IsRequired();
        builder.Property(ingredient => ingredient.Position)
            .HasColumnType("INTEGER")
            .IsRequired();
    }
}