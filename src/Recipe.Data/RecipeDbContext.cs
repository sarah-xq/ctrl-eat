using Microsoft.EntityFrameworkCore;
using Recipe.Domain;

namespace Recipe.Data;
public sealed class RecipeDbContext(DbContextOptions<RecipeDbContext> options) : DbContext(options)
{
    public DbSet<Recipe.Domain.Recipe> Recipes => Set<Recipe.Domain.Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        var recipe = builder.Entity<Recipe.Domain.Recipe>();
        recipe.HasKey(x => x.Id);
        recipe.Property(x => x.Name).IsRequired().HasMaxLength(200);
        recipe.Property(x => x.TotalCalories).HasPrecision(18, 3);
        recipe.HasMany(x => x.Ingredients).WithOne().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Cascade);
        var ingredient = builder.Entity<RecipeIngredient>();
        ingredient.HasKey(x => x.Id);
        ingredient.HasIndex(x => new { x.RecipeId, x.IngredientId }).IsUnique();
        ingredient.Property(x => x.Quantity).HasPrecision(18, 3);
        ingredient.Property(x => x.CaloriesPerUnit).HasPrecision(18, 3);
    }
}
