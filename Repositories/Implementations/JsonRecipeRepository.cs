using System;
using System.Collections.Generic;
using System.Linq;
using CtrlEat.Models;
using CtrlEat.Repositories.Interfaces;

namespace CtrlEat.Repositories.Implementations
{
    /// <summary>
    /// JSON file-based implementation of Recipe repository
    /// Persists recipe data to Data/recipes.json
    /// RecipeIngredients are stored as nested objects within recipes
    /// </summary>
    public class JsonRecipeRepository : IRecipeRepository
    {
        private readonly JsonFileHelper _recipeFileHelper;
        private readonly JsonFileHelper _recipeIngredientFileHelper;
        private int _nextRecipeId = 1;
        private int _nextRecipeIngredientId = 1;

        public JsonRecipeRepository(
            string recipeFilePath = "Data/recipes.json",
            string recipeIngredientFilePath = "Data/recipeIngredients.json")
        {
            _recipeFileHelper = new JsonFileHelper(recipeFilePath);
            _recipeIngredientFileHelper = new JsonFileHelper(recipeIngredientFilePath);
            InitializeNextIds();
        }

        /// <summary>
        /// Initialize the next IDs by finding the highest existing IDs
        /// </summary>
        private void InitializeNextIds()
        {
            var recipes = _recipeFileHelper.ReadData<Recipe>();
            if (recipes.Count > 0)
            {
                _nextRecipeId = recipes.Max(r => r.Id) + 1;
            }

            var recipeIngredients = _recipeIngredientFileHelper.ReadData<RecipeIngredient>();
            if (recipeIngredients.Count > 0)
            {
                _nextRecipeIngredientId = recipeIngredients.Max(ri => ri.Id) + 1;
            }
        }

        public Recipe GetById(int id)
        {
            var recipes = _recipeFileHelper.ReadData<Recipe>();
            var recipe = recipes.FirstOrDefault(r => r.Id == id);
            if (recipe != null)
            {
                recipe.RecipeIngredients = GetRecipeIngredients(id);
            }
            return recipe;
        }

        public List<Recipe> GetAll()
        {
            var recipes = _recipeFileHelper.ReadData<Recipe>();
            foreach (var recipe in recipes)
            {
                recipe.RecipeIngredients = GetRecipeIngredients(recipe.Id);
            }
            return recipes;
        }

        public List<Recipe> GetByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<Recipe>();

            var recipes = _recipeFileHelper.ReadData<Recipe>();
            var results = recipes
                .Where(r => r.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var recipe in results)
            {
                recipe.RecipeIngredients = GetRecipeIngredients(recipe.Id);
            }
            return results;
        }

        public List<Recipe> GetByAuthor(string author)
        {
            if (string.IsNullOrWhiteSpace(author))
                return new List<Recipe>();

            var recipes = _recipeFileHelper.ReadData<Recipe>();
            var results = recipes
                .Where(r => r.Author != null && r.Author.Contains(author, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var recipe in results)
            {
                recipe.RecipeIngredients = GetRecipeIngredients(recipe.Id);
            }
            return results;
        }

        public int Create(Recipe recipe)
        {
            if (recipe == null)
                throw new ArgumentNullException(nameof(recipe));

            var recipes = _recipeFileHelper.ReadData<Recipe>();
            recipe.Id = _nextRecipeId++;
            recipe.CreatedAt = DateTime.UtcNow;
            recipe.UpdatedAt = DateTime.UtcNow;
            recipe.RecipeIngredients = recipe.RecipeIngredients ?? new List<RecipeIngredient>();
            recipes.Add(recipe);
            _recipeFileHelper.WriteData(recipes);
            return recipe.Id;
        }

        public void Update(Recipe recipe)
        {
            if (recipe == null)
                throw new ArgumentNullException(nameof(recipe));

            var recipes = _recipeFileHelper.ReadData<Recipe>();
            var existing = recipes.FirstOrDefault(r => r.Id == recipe.Id);
            if (existing == null)
                throw new InvalidOperationException($"Recipe with id {recipe.Id} not found");

            existing.Name = recipe.Name;
            existing.Author = recipe.Author;
            existing.TotalTimeMinutes = recipe.TotalTimeMinutes;
            existing.CookTimeMinutes = recipe.CookTimeMinutes;
            existing.Description = recipe.Description;
            existing.TotalCalories = recipe.TotalCalories;
            existing.UpdatedAt = DateTime.UtcNow;
            _recipeFileHelper.WriteData(recipes);
        }

        public void Delete(int id)
        {
            var recipes = _recipeFileHelper.ReadData<Recipe>();
            var recipe = recipes.FirstOrDefault(r => r.Id == id);
            if (recipe == null)
                throw new InvalidOperationException($"Recipe with id {id} not found");

            recipes.Remove(recipe);
            _recipeFileHelper.WriteData(recipes);

            // Remove associated recipe ingredients
            var recipeIngredients = _recipeIngredientFileHelper.ReadData<RecipeIngredient>();
            var toRemove = recipeIngredients.Where(ri => ri.RecipeId == id).ToList();
            foreach (var ri in toRemove)
            {
                recipeIngredients.Remove(ri);
            }
            _recipeIngredientFileHelper.WriteData(recipeIngredients);
        }

        public void AddIngredient(int recipeId, RecipeIngredient recipeIngredient)
        {
            if (recipeIngredient == null)
                throw new ArgumentNullException(nameof(recipeIngredient));

            var recipes = _recipeFileHelper.ReadData<Recipe>();
            var recipe = recipes.FirstOrDefault(r => r.Id == recipeId);
            if (recipe == null)
                throw new InvalidOperationException($"Recipe with id {recipeId} not found");

            var recipeIngredients = _recipeIngredientFileHelper.ReadData<RecipeIngredient>();
            recipeIngredient.Id = _nextRecipeIngredientId++;
            recipeIngredient.RecipeId = recipeId;
            recipeIngredients.Add(recipeIngredient);
            _recipeIngredientFileHelper.WriteData(recipeIngredients);
        }

        public void RemoveIngredient(int recipeId, int ingredientId)
        {
            var recipeIngredients = _recipeIngredientFileHelper.ReadData<RecipeIngredient>();
            var recipeIngredient = recipeIngredients
                .FirstOrDefault(ri => ri.RecipeId == recipeId && ri.IngredientId == ingredientId);

            if (recipeIngredient != null)
            {
                recipeIngredients.Remove(recipeIngredient);
                _recipeIngredientFileHelper.WriteData(recipeIngredients);
            }
        }

        public List<RecipeIngredient> GetRecipeIngredients(int recipeId)
        {
            var recipeIngredients = _recipeIngredientFileHelper.ReadData<RecipeIngredient>();
            return recipeIngredients
                .Where(ri => ri.RecipeId == recipeId)
                .ToList();
        }
    }
}
