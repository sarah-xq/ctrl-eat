using GroceryList.Api.Clients;
using GroceryList.Api.Services;
using GroceryList.Data;
using GroceryList.Data.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("GroceryList")
    ?? throw new InvalidOperationException("ConnectionStrings:GroceryList is required.");
var ingredientApiBaseUrl = builder.Configuration["IngredientApi:BaseUrl"];
if (!Uri.TryCreate(ingredientApiBaseUrl, UriKind.Absolute, out var ingredientApiUri)
    || ingredientApiUri.Scheme is not ("http" or "https"))
{
    throw new InvalidOperationException("IngredientApi:BaseUrl must be configured as an absolute HTTP or HTTPS URL.");
}

builder.Services.AddDbContext<GroceryListDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IGroceryListRepository, GroceryListRepository>();
builder.Services.AddScoped<GroceryListService>();
var ingredientHttpClient = builder.Services.AddHttpClient<HttpIngredientClient>(client =>
{
    client.BaseAddress = ingredientApiUri;
});
if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
{
    ingredientHttpClient.ConfigurePrimaryHttpMessageHandler(() => new DevelopmentIngredientHandler());
}

builder.Services.AddTransient<IIngredientClient>(services => services.GetRequiredService<HttpIngredientClient>());
builder.Services.AddExceptionHandler<GroceryListExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();

var app = builder.Build();
app.UseExceptionHandler();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<GroceryListDbContext>().Database.Migrate();
}

app.MapControllers();
app.Run();

public partial class Program;
