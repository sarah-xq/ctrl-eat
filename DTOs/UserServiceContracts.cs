using System;

namespace CtrlEat.DTOs
{
    /// <summary>
    /// DTOs for User Service
    /// All requests/responses for user operations are contained here.
    /// </summary>
    public class CreateUserRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int CalorieGoal { get; set; }
    }

    public class UpdateUserRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } // optional, if present will update
        public int? CalorieGoal { get; set; }
    }

    public class UserResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int CalorieGoal { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class GetUserRequest
    {
        public int? Id { get; set; }
        public string Email { get; set; }
    }
}
