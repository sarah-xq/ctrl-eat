using System;

namespace CtrlEat.Models
{
    /// <summary>
    /// Domain model for User
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }

        // Stored as hashed password with salt
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }

        public int CalorieGoal { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
