
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

            // Register services
            builder.Services.AddSingleton<IngredientService>();
            builder.Services.AddSingleton<RecipeService>();

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
