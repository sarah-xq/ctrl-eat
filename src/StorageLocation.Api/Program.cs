using Microsoft.EntityFrameworkCore;
using StorageLocation.Api.Services;
using StorageLocation.Data;
using StorageLocation.Data.Repositories;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var connectionString = builder.Configuration.GetConnectionString("StorageLocation")
            ?? throw new InvalidOperationException("ConnectionStrings:StorageLocation is required.");

        builder.Services.AddDbContext<StorageLocationDbContext>(options => options.UseSqlite(connectionString));
        builder.Services.AddScoped<IStorageLocationRepository, StorageLocationRepository>();
        builder.Services.AddScoped<StorageLocationService>();
        builder.Services.AddControllers();

        var app = builder.Build();
        using (var scope = app.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<StorageLocationDbContext>().Database.Migrate();
        }

        app.MapControllers();
        app.Run();
    }
}
