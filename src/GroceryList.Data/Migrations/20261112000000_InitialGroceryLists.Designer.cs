using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using GroceryList.Data;
using GroceryList.Data.Entities;

namespace GroceryList.Data.Migrations;

[DbContext(typeof(GroceryListDbContext))]
[Migration("20261112000000_InitialGroceryLists")]
partial class InitialGroceryLists
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.14");
        modelBuilder.Entity<GroceryListRecord>(entity =>
        {
            entity.Property(list => list.Id).ValueGeneratedOnAdd().HasColumnType("INTEGER");
            entity.Property(list => list.Date).HasColumnType("TEXT").IsRequired();
            entity.Property(list => list.CreatedAt)
                .HasConversion(value => value.Ticks, ticks => new DateTime(ticks, DateTimeKind.Utc))
                .HasColumnType("INTEGER")
                .IsRequired();
            entity.Property(list => list.UpdatedAt)
                .HasConversion(value => value.Ticks, ticks => new DateTime(ticks, DateTimeKind.Utc))
                .HasColumnType("INTEGER")
                .IsRequired();
            entity.HasKey(list => list.Id);
            entity.ToTable("GroceryLists");
        });

        modelBuilder.Entity<GroceryListIngredientRecord>(entity =>
        {
            entity.Property(ingredient => ingredient.GroceryListId).HasColumnType("INTEGER");
            entity.Property(ingredient => ingredient.IngredientId).HasColumnType("INTEGER").IsRequired();
            entity.Property(ingredient => ingredient.Position).HasColumnType("INTEGER").IsRequired();
            entity.HasKey(ingredient => new { ingredient.GroceryListId, ingredient.IngredientId });
            entity.HasIndex(ingredient => new { ingredient.GroceryListId, ingredient.Position });
            entity.ToTable("GroceryListIngredients", table =>
                table.HasCheckConstraint("CK_GroceryListIngredients_IngredientId_Positive", "\"IngredientId\" > 0"));
        });

        modelBuilder.Entity<GroceryListRecord>()
            .HasMany(list => list.IngredientReferences)
            .WithOne()
            .HasForeignKey(ingredient => ingredient.GroceryListId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}