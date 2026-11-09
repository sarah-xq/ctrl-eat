using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StorageLocation.Data;

public sealed class DesignTimeStorageLocationDbContextFactory : IDesignTimeDbContextFactory<StorageLocationDbContext>
{
    public StorageLocationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<StorageLocationDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        return new StorageLocationDbContext(options);
    }
}
