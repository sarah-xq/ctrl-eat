using GroceryList.Api.Clients;
using GroceryList.Api.Services;
using GroceryList.Data;
using GroceryList.Data.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("GroceryList")
    ?? throw new InvalidOperationException("ConnectionStrings:GroceryList is required.");

builder.Services.AddDbContext<GroceryListDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IGroceryListRepository, GroceryListRepository>();
builder.Services.AddScoped<GroceryListService>();
builder.Services.AddSingleton<IIngredientClient, UnavailableIngredientClient>();
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
