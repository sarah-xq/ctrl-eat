using System.Collections.Generic;
using CtrlEat.Models;

namespace CtrlEat.Repositories.Interfaces
{
    /// <summary>
    /// Interface for Ingredient data access
    /// Defines contract for ingredient persistence operations
    /// </summary>
    public interface IIngredientRepository
    {
        Ingredient GetById(int id);
        List<Ingredient> GetAll();
        List<Ingredient> GetByName(string name);
        List<Ingredient> GetByFoodGroup(string foodGroup);
        int Create(Ingredient ingredient);
        void Update(Ingredient ingredient);
        void Delete(int id);
    }
}
