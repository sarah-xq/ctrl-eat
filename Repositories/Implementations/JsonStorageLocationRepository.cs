using System;
using System.Collections.Generic;
using System.Linq;
using CtrlEat.Models;
using CtrlEat.Repositories.Interfaces;

namespace CtrlEat.Repositories.Implementations
{
    /// <summary>
    /// JSON data-access implementation for storage locations.
    /// </summary>
    public class JsonStorageLocationRepository : IStorageLocationRepository
    {
        private readonly JsonFileHelper _fileHelper;
        private int _nextId = 1;

        public JsonStorageLocationRepository(string filePath = "Data/storageLocations.json")
        {
            _fileHelper = new JsonFileHelper(filePath);
            InitializeNextId();
        }

        private void InitializeNextId()
        {
            var data = _fileHelper.ReadData<StorageLocation>();
            if (data.Count > 0)
                _nextId = data.Max(location => location.Id) + 1;
        }

        public StorageLocation GetById(int id)
        {
            return _fileHelper.ReadData<StorageLocation>()
                .FirstOrDefault(location => location.Id == id);
        }

        public List<StorageLocation> GetAll()
        {
            return _fileHelper.ReadData<StorageLocation>();
        }

        public List<StorageLocation> GetByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<StorageLocation>();

            return _fileHelper.ReadData<StorageLocation>()
                .Where(location => location.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public List<StorageLocation> GetByType(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return new List<StorageLocation>();

            return _fileHelper.ReadData<StorageLocation>()
                .Where(location => location.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public int Create(StorageLocation location)
        {
            if (location == null) throw new ArgumentNullException(nameof(location));

            var data = _fileHelper.ReadData<StorageLocation>();
            location.Id = _nextId++;
            location.CreatedAt = DateTime.UtcNow;
            location.UpdatedAt = DateTime.UtcNow;
            data.Add(location);
            _fileHelper.WriteData(data);
            return location.Id;
        }

        public void Update(StorageLocation location)
        {
            if (location == null) throw new ArgumentNullException(nameof(location));

            var data = _fileHelper.ReadData<StorageLocation>();
            var existing = data.FirstOrDefault(item => item.Id == location.Id);
            if (existing == null)
                throw new InvalidOperationException($"Storage location with id {location.Id} not found");

            existing.Name = location.Name;
            existing.Type = location.Type;
            existing.MinimumTemperature = location.MinimumTemperature;
            existing.MaximumTemperature = location.MaximumTemperature;
            existing.IngredientIds = location.IngredientIds;
            existing.UpdatedAt = DateTime.UtcNow;
            _fileHelper.WriteData(data);
        }

        public void Delete(int id)
        {
            var data = _fileHelper.ReadData<StorageLocation>();
            var existing = data.FirstOrDefault(location => location.Id == id);
            if (existing == null)
                throw new InvalidOperationException($"Storage location with id {id} not found");

            data.Remove(existing);
            _fileHelper.WriteData(data);
        }
    }
}
