using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using CtrlEat.DTOs;
using CtrlEat.Models;
using CtrlEat.Repositories.Interfaces;

namespace CtrlEat.Services
{
    public class UserService
    {
        private readonly IUserRepository _repository;

        public UserService(IUserRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public UserResponse CreateUser(CreateUserRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Email)) throw new ArgumentException("Email is required");
            if (string.IsNullOrWhiteSpace(request.Password)) throw new ArgumentException("Password is required");

            // Check duplicate email
            var existing = _repository.GetByEmail(request.Email);
            if (existing != null) throw new InvalidOperationException("A user with the same email already exists");

            // Hash password
            var salt = GenerateSalt();
            var hash = HashPassword(request.Password, salt);

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = Convert.ToBase64String(hash),
                PasswordSalt = Convert.ToBase64String(salt),
                CalorieGoal = request.CalorieGoal
            };

            var id = _repository.Create(user);
            user.Id = id;

            return MapToResponse(user);
        }

        public UserResponse GetUser(int id)
        {
            var user = _repository.GetById(id);
            if (user == null) throw new InvalidOperationException($"User with id {id} not found");
            return MapToResponse(user);
        }

        public UserResponse GetUserByEmail(string email)
        {
            var user = _repository.GetByEmail(email);
            if (user == null) throw new InvalidOperationException($"User with email {email} not found");
            return MapToResponse(user);
        }

        public List<UserResponse> GetAllUsers()
        {
            return _repository.GetAll().Select(MapToResponse).ToList();
        }

        public UserResponse UpdateUser(int id, UpdateUserRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var user = _repository.GetById(id);
            if (user == null) throw new InvalidOperationException($"User with id {id} not found");

            if (!string.IsNullOrWhiteSpace(request.Name)) user.Name = request.Name;
            if (!string.IsNullOrWhiteSpace(request.Email)) user.Email = request.Email;
            if (request.CalorieGoal.HasValue) user.CalorieGoal = request.CalorieGoal.Value;

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                var salt = GenerateSalt();
                var hash = HashPassword(request.Password, salt);
                user.PasswordSalt = Convert.ToBase64String(salt);
                user.PasswordHash = Convert.ToBase64String(hash);
            }

            _repository.Update(user);
            return MapToResponse(user);
        }

        public void DeleteUser(int id)
        {
            _repository.Delete(id);
        }

        // Internal helpers
        private static UserResponse MapToResponse(User user)
        {
            return new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                CalorieGoal = user.CalorieGoal,
                CreatedAt = user.CreatedAt
            };
        }

        private static byte[] GenerateSalt(int size = 16)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[size];
            rng.GetBytes(salt);
            return salt;
        }

        private static byte[] HashPassword(string password, byte[] salt, int iterations = 10000, int length = 32)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(length);
        }
    }
}
