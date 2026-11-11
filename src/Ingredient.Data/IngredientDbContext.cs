using Ingredient.Domain;
using Microsoft.EntityFrameworkCore;

namespace Ingredient.Data;

public sealed class IngredientDbContext(DbContextOptions<IngredientDbContext> options) : DbContext(options)
{
    public DbSet<Ingredient.Domain.Ingredient> Ingredients => Set<Ingredient.Domain.Ingredient>();
    public DbSet<NutritionalInfo> NutritionalInfos => Set<NutritionalInfo>();
    public DbSet<Allergen> Allergens => Set<Allergen>();
    public DbSet<IngredientAllergen> IngredientAllergens => Set<IngredientAllergen>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        var entity = builder.Entity<Ingredient.Domain.Ingredient>();
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
        entity.Property(x => x.FoodGroup).IsRequired().HasMaxLength(100);
        entity.Property(x => x.Calories).HasPrecision(18, 3);
        entity.Property(x => x.StorageMethod).HasMaxLength(30).HasDefaultValue("RoomTemperature");
        entity.Property(x => x.BatchNumber).HasMaxLength(100);
        entity.HasIndex(x => x.Name);
        entity.HasOne(x => x.Nutrition).WithOne().HasForeignKey<NutritionalInfo>(n => n.IngredientId)
            .OnDelete(DeleteBehavior.Cascade);

        var nutrition = builder.Entity<NutritionalInfo>();
        nutrition.HasKey(x => x.IngredientId);
        nutrition.Property(x => x.ServingSize).IsRequired().HasMaxLength(100);
        nutrition.Property(x => x.DataSource).HasMaxLength(200);
        nutrition.Property(x => x.VitaminsMineralsJson).IsRequired();
        foreach (var property in new[] { nameof(NutritionalInfo.ProteinGrams), nameof(NutritionalInfo.CarbohydratesGrams),
            nameof(NutritionalInfo.FatGrams), nameof(NutritionalInfo.FiberGrams), nameof(NutritionalInfo.SugarGrams),
            nameof(NutritionalInfo.SodiumMilligrams), nameof(NutritionalInfo.CholesterolMilligrams) })
            nutrition.Property<decimal>(property).HasPrecision(18, 3);

        var allergen = builder.Entity<Allergen>();
        allergen.HasKey(x => x.Id);
        allergen.Property(x => x.Name).IsRequired().HasMaxLength(100);
        allergen.HasIndex(x => x.Name).IsUnique();
        var join = builder.Entity<IngredientAllergen>();
        join.HasKey(x => new { x.IngredientId, x.AllergenId });
        join.HasOne<Ingredient.Domain.Ingredient>().WithMany(x => x.Allergens)
            .HasForeignKey(x => x.IngredientId).OnDelete(DeleteBehavior.Cascade);
        join.HasOne(x => x.Allergen).WithMany().HasForeignKey(x => x.AllergenId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
