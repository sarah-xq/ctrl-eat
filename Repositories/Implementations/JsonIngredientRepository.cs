using System;
using System.Collections.Generic;
using System.Linq;
using CtrlEat.Models;
using CtrlEat.Repositories.Interfaces;

namespace CtrlEat.Repositories.Implementations
{
    /// <summary>
    /// JSON file-based implementation of Ingredient repository
    /// Persists ingredient data to Data/ingredients.json
    /// </summary>
    public class JsonIngredientRepository : IIngredientRepository
    {
        private readonly JsonFileHelper _fileHelper;
        private int _nextId = 1;

        public JsonIngredientRepository(string filePath = "Data/ingredients.json")
        {
            _fileHelper = new JsonFileHelper(filePath);
            InitializeNextId();
        }

        /// <summary>
        /// Initialize the next ID by finding the highest existing ID
        /// </summary>
        private void InitializeNextId()
        {
            var data = _fileHelper.ReadData<Ingredient>();
            if (data.Count > 0)
            {
                _nextId = data.Max(i => i.Id) + 1;
            }
        }

        public Ingredient GetById(int id)
        {
            var data = _fileHelper.ReadData<Ingredient>();
            return data.FirstOrDefault(i => i.Id == id);
        }

        public List<Ingredient> GetAll()
        {
            return _fileHelper.ReadData<Ingredient>();
        }

        public List<Ingredient> GetByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<Ingredient>();

            var data = _fileHelper.ReadData<Ingredient>();
            return data
                .Where(i => i.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public List<Ingredient> GetByFoodGroup(string foodGroup)
        {
            if (string.IsNullOrWhiteSpace(foodGroup))
                return new List<Ingredient>();

            var data = _fileHelper.ReadData<Ingredient>();
            return data
                .Where(i => i.FoodGroup.Equals(foodGroup, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public int Create(Ingredient ingredient)
        {
            if (ingredient == null)
                throw new ArgumentNullException(nameof(ingredient));

            var data = _fileHelper.ReadData<Ingredient>();
            ingredient.Id = _nextId++;
            ingredient.CreatedAt = DateTime.UtcNow;
            ingredient.UpdatedAt = DateTime.UtcNow;
            data.Add(ingredient);
            _fileHelper.WriteData(data);
            return ingredient.Id;
        }

        public void Update(Ingredient ingredient)
        {
            if (ingredient == null)
                throw new ArgumentNullException(nameof(ingredient));

            var data = _fileHelper.ReadData<Ingredient>();
            var existing = data.FirstOrDefault(i => i.Id == ingredient.Id);
            if (existing == null)
                throw new InvalidOperationException($"Ingredient with id {ingredient.Id} not found");

            existing.Name = ingredient.Name;
            existing.FoodGroup = ingredient.FoodGroup;
            existing.ExpiryDate = ingredient.ExpiryDate;
            existing.Calories = ingredient.Calories;
            existing.UpdatedAt = DateTime.UtcNow;
            _fileHelper.WriteData(data);
        }

        public void Delete(int id)
        {
            var data = _fileHelper.ReadData<Ingredient>();
            var ingredient = data.FirstOrDefault(i => i.Id == id);
            if (ingredient == null)
                throw new InvalidOperationException($"Ingredient with id {id} not found");

            data.Remove(ingredient);
            _fileHelper.WriteData(data);
        }
    }
}
