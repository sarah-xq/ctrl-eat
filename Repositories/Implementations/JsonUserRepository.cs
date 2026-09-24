using System;
using System.Collections.Generic;
using System.Linq;
using CtrlEat.Models;
using CtrlEat.Repositories.Interfaces;

namespace CtrlEat.Repositories.Implementations
{
    public class JsonUserRepository : IUserRepository
    {
        private readonly JsonFileHelper _fileHelper;
        private int _nextId = 1;

        public JsonUserRepository(string filePath = "Data/users.json")
        {
            _fileHelper = new JsonFileHelper(filePath);
            InitializeNextId();
        }

        private void InitializeNextId()
        {
            var list = _fileHelper.ReadData<User>();
            if (list.Count > 0)
                _nextId = list.Max(u => u.Id) + 1;
        }

        public int Create(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            var list = _fileHelper.ReadData<User>();
            user.Id = _nextId++;
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            list.Add(user);
            _fileHelper.WriteData(list);
            return user.Id;
        }

        public void Delete(int id)
        {
            var list = _fileHelper.ReadData<User>();
            var existing = list.FirstOrDefault(u => u.Id == id);
            if (existing == null) throw new InvalidOperationException($"User with id {id} not found");
            list.Remove(existing);
            _fileHelper.WriteData(list);
        }

        public List<User> GetAll()
        {
            return _fileHelper.ReadData<User>();
        }

        public User GetByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            var list = _fileHelper.ReadData<User>();
            return list.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
        }

        public User GetById(int id)
        {
            var list = _fileHelper.ReadData<User>();
            return list.FirstOrDefault(u => u.Id == id);
        }

        public void Update(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            var list = _fileHelper.ReadData<User>();
            var existing = list.FirstOrDefault(u => u.Id == user.Id);
            if (existing == null) throw new InvalidOperationException($"User with id {user.Id} not found");

            existing.Name = user.Name;
            existing.Email = user.Email;
            existing.PasswordHash = user.PasswordHash;
            existing.PasswordSalt = user.PasswordSalt;
            existing.CalorieGoal = user.CalorieGoal;
            existing.UpdatedAt = DateTime.UtcNow;

            _fileHelper.WriteData(list);
        }
    }
}
