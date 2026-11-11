using Ingredient.Domain;
using Microsoft.EntityFrameworkCore;

namespace Ingredient.Data;

public sealed class IngredientDbContext(DbContextOptions<IngredientDbContext> options) : DbContext(options)
{
    public DbSet<Ingredient.Domain.Ingredient> Ingredients => Set<Ingredient.Domain.Ingredient>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        var entity = builder.Entity<Ingredient.Domain.Ingredient>();
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
        entity.Property(x => x.FoodGroup).IsRequired().HasMaxLength(100);
        entity.Property(x => x.Calories).HasPrecision(18, 3);
        entity.HasIndex(x => x.Name);
    }
}
