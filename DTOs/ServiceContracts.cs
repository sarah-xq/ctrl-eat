using System;
using System.Collections.Generic;

namespace CtrlEat.DTOs
{
    // ============ INGREDIENT SERVICE CONTRACTS ============

    /// <summary>
    /// Request to create or update an ingredient
    /// </summary>
    public class CreateIngredientRequest
    {
        public string Name { get; set; }
        public string FoodGroup { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal Calories { get; set; }
    }

    /// <summary>
    /// Response containing ingredient details
    /// </summary>
    public class IngredientResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FoodGroup { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal Calories { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Request to retrieve ingredients
    /// </summary>
    public class GetIngredientsRequest
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string FoodGroup { get; set; }
    }


    // ============ RECIPE SERVICE CONTRACTS ============

    /// <summary>
    /// Represents an ingredient with quantity in a recipe
    /// </summary>
    public class RecipeIngredientRequest
    {
        public int IngredientId { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; } // e.g., "grams", "cups", "ml"
    }

    /// <summary>
    /// Request to create or update a recipe
    /// </summary>
    public class CreateRecipeRequest
    {
        public string Name { get; set; }
        public string Author { get; set; } // Optional
        public int TotalTimeMinutes { get; set; }
        public int CookTimeMinutes { get; set; }
        public string Description { get; set; }
        public List<RecipeIngredientRequest> Ingredients { get; set; } = new List<RecipeIngredientRequest>();
    }

    /// <summary>
    /// Represents an ingredient in a recipe response
    /// </summary>
    public class RecipeIngredientResponse
    {
        public int IngredientId { get; set; }
        public string IngredientName { get; set; }
        public string FoodGroup { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
        public decimal CaloriesPerUnit { get; set; }
    }

    /// <summary>
    /// Response containing recipe details
    /// </summary>
    public class RecipeResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public int TotalTimeMinutes { get; set; }
        public int CookTimeMinutes { get; set; }
        public string Description { get; set; }
        public decimal TotalCalories { get; set; }
        public List<RecipeIngredientResponse> Ingredients { get; set; } = new List<RecipeIngredientResponse>();
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Request to retrieve recipes
    /// </summary>
    public class GetRecipesRequest
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
    }

    /// <summary>
    /// Inter-service communication: Recipe to Ingredient service
    /// Used internally when recipe service needs ingredient details
    /// </summary>
    public class IngredientLookupRequest
    {
        public int IngredientId { get; set; }
    }

    /// <summary>
    /// Inter-service communication: Response from Ingredient service to Recipe service
    /// </summary>
    public class IngredientLookupResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FoodGroup { get; set; }
        public decimal Calories { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
