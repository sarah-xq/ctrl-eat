
using CtrlEat.Repositories.Implementations;
using CtrlEat.Repositories.Interfaces;
using CtrlEat.Services;

namespace CtrlEat
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllers();

            // Add Swagger/OpenAPI
            builder.Services.AddSwaggerGen();
            builder.Services.AddEndpointsApiExplorer();

            // Register repositories
            builder.Services.AddSingleton<IIngredientRepository>(
                new JsonIngredientRepository("Data/ingredients.json"));
            builder.Services.AddSingleton<IRecipeRepository>(
                new JsonRecipeRepository("Data/recipes.json", "Data/recipeIngredients.json"));
            builder.Services.AddSingleton<IUserRepository>(
                new JsonUserRepository("Data/users.json"));
            builder.Services.AddSingleton<IDailyMealPlanRepository>(
                new JsonDailyMealPlanRepository("Data/dailyMealPlans.json"));
            builder.Services.AddSingleton<IGroceryListRepository>(
                new JsonGroceryListRepository("Data/groceryLists.json"));
            builder.Services.AddSingleton<IStorageLocationRepository>(
                new JsonStorageLocationRepository("Data/storageLocations.json"));

            // Register services
            builder.Services.AddSingleton<IngredientService>();
            builder.Services.AddSingleton<RecipeService>();
            builder.Services.AddSingleton<UserService>();
            builder.Services.AddSingleton<DailyMealPlanService>();
            builder.Services.AddSingleton<GroceryListService>();
            builder.Services.AddSingleton<StorageLocationService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
