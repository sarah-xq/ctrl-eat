using Microsoft.EntityFrameworkCore;
using StorageLocation.Api.Clients;
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
        var ingredientApiBaseUrl = builder.Configuration["IngredientApi:BaseUrl"];
        if (!Uri.TryCreate(ingredientApiBaseUrl, UriKind.Absolute, out var ingredientApiUri)
            || ingredientApiUri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException("IngredientApi:BaseUrl must be configured as an absolute HTTP or HTTPS URL.");
        }

        builder.Services.AddDbContext<StorageLocationDbContext>(options => options.UseSqlite(connectionString));
        builder.Services.AddScoped<IStorageLocationRepository, StorageLocationRepository>();
        builder.Services.AddScoped<StorageLocationService>();
        var ingredientClient = builder.Services.AddHttpClient<HttpIngredientClient>(client =>
        {
            client.BaseAddress = ingredientApiUri;
        });
        if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
        {
            ingredientClient.ConfigurePrimaryHttpMessageHandler(() => new DevelopmentIngredientHandler());
        }

        builder.Services.AddTransient<IIngredientClient>(services => services.GetRequiredService<HttpIngredientClient>());
        builder.Services.AddControllers();

        var app = builder.Build();
        app.UseExceptionHandler(errorApp => errorApp.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Task.CompletedTask;
        }));

        using (var scope = app.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<StorageLocationDbContext>().Database.Migrate();
        }

        app.MapControllers();
        app.Run();
    }
}
