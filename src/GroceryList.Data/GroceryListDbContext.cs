using Microsoft.EntityFrameworkCore;
using GroceryList.Data.Entities;

namespace GroceryList.Data;

public sealed class GroceryListDbContext(DbContextOptions<GroceryListDbContext> options) : DbContext(options)
{
    public DbSet<GroceryListRecord> GroceryLists => Set<GroceryListRecord>();
    public DbSet<GroceryListIngredientRecord> GroceryListIngredients => Set<GroceryListIngredientRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GroceryListDbContext).Assembly);
    }
}