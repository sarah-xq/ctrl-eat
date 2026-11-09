using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GroceryList.Data;

public sealed class DesignTimeGroceryListDbContextFactory : IDesignTimeDbContextFactory<GroceryListDbContext>
{
    public GroceryListDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<GroceryListDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        return new GroceryListDbContext(options);
    }
}