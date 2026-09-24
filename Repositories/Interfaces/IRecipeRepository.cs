using System.Collections.Generic;
using CtrlEat.Models;

namespace CtrlEat.Repositories.Interfaces
{
    /// <summary>
    /// Interface for Recipe data access
    /// Defines contract for recipe persistence operations
    /// </summary>
    public interface IRecipeRepository
    {
        Recipe GetById(int id);
        List<Recipe> GetAll();
        List<Recipe> GetByName(string name);
        List<Recipe> GetByAuthor(string author);
        int Create(Recipe recipe);
        void Update(Recipe recipe);
        void Delete(int id);
        void AddIngredient(int recipeId, RecipeIngredient recipeIngredient);
        void RemoveIngredient(int recipeId, int ingredientId);
        List<RecipeIngredient> GetRecipeIngredients(int recipeId);
    }
}
