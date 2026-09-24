using System;
using System.Collections.Generic;
using System.Linq;
using CtrlEat.Models;
using CtrlEat.Repositories.Interfaces;

namespace CtrlEat.Repositories.Implementations
{
    /// <summary>
    /// JSON data-access implementation for grocery lists.
    /// </summary>
    public class JsonGroceryListRepository : IGroceryListRepository
    {
        private readonly JsonFileHelper _fileHelper;
        private int _nextId = 1;

        public JsonGroceryListRepository(string filePath = "Data/groceryLists.json")
        {
            _fileHelper = new JsonFileHelper(filePath);
            InitializeNextId();
        }

        private void InitializeNextId()
        {
            var data = _fileHelper.ReadData<GroceryList>();
            if (data.Count > 0)
                _nextId = data.Max(list => list.Id) + 1;
        }

        public GroceryList GetById(int id)
        {
            return _fileHelper.ReadData<GroceryList>()
                .FirstOrDefault(list => list.Id == id);
        }

        public List<GroceryList> GetAll()
        {
            return _fileHelper.ReadData<GroceryList>();
        }

        public List<GroceryList> GetByDate(DateTime date)
        {
            return _fileHelper.ReadData<GroceryList>()
                .Where(list => list.Date.Date == date.Date)
                .ToList();
        }

        public int Create(GroceryList groceryList)
        {
            if (groceryList == null) throw new ArgumentNullException(nameof(groceryList));

            var data = _fileHelper.ReadData<GroceryList>();
            groceryList.Id = _nextId++;
            groceryList.CreatedAt = DateTime.UtcNow;
            groceryList.UpdatedAt = DateTime.UtcNow;
            data.Add(groceryList);
            _fileHelper.WriteData(data);
            return groceryList.Id;
        }

        public void Update(GroceryList groceryList)
        {
            if (groceryList == null) throw new ArgumentNullException(nameof(groceryList));

            var data = _fileHelper.ReadData<GroceryList>();
            var existing = data.FirstOrDefault(list => list.Id == groceryList.Id);
            if (existing == null)
                throw new InvalidOperationException($"Grocery list with id {groceryList.Id} not found");

            existing.IngredientIds = groceryList.IngredientIds;
            existing.Date = groceryList.Date;
            existing.UpdatedAt = DateTime.UtcNow;
            _fileHelper.WriteData(data);
        }

        public void Delete(int id)
        {
            var data = _fileHelper.ReadData<GroceryList>();
            var existing = data.FirstOrDefault(list => list.Id == id);
            if (existing == null)
                throw new InvalidOperationException($"Grocery list with id {id} not found");

            data.Remove(existing);
            _fileHelper.WriteData(data);
        }
    }
}
