using System;
using System.Collections.Generic;

namespace CtrlEat.Models
{
    /// <summary>
    /// Domain model for Daily Meal Plan
    /// </summary>
    public class DailyMealPlan
    {
        public int Id { get; set; }

        // Foreign key to User
        public int UserId { get; set; }

        // Reference recipes by id
        public int? BreakfastRecipeId { get; set; }
        public int? LunchRecipeId { get; set; }
        public int? DinnerRecipeId { get; set; }

        // Snacks may reference either a Recipe or an Ingredient
        public List<SnackItem> Snacks { get; set; } = new List<SnackItem>();

        public DateTime Date { get; set; }

        public decimal TotalCalories { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public DailyMealPlan()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Represents a snack which can be either a Recipe or an Ingredient
    /// </summary>
    public class SnackItem
    {
        public int Id { get; set; }
        public bool IsRecipe { get; set; }
        public int ItemId { get; set; } // recipe id or ingredient id
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
    }
}
