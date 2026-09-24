using System;
using System.Collections.Generic;

namespace CtrlEat.Models
{
    /// <summary>
    /// Domain model for Recipe
    /// Represents the core entity in the Recipe Service
    /// </summary>
    public class Recipe
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Author { get; set; } // Optional
        public int TotalTimeMinutes { get; set; }
        public int CookTimeMinutes { get; set; }
        public string Description { get; set; }
        public decimal TotalCalories { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation: many-to-many relationship with Ingredient
        public List<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();

        public Recipe()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Join entity for many-to-many relationship between Recipe and Ingredient
    /// Stores quantity information for each ingredient in a recipe
    /// </summary>
    public class RecipeIngredient
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public int IngredientId { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; } // e.g., "grams", "cups", "ml"

        // Foreign key navigation
        public Recipe Recipe { get; set; }
    }
}
