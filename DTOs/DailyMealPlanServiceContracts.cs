using System;
using System.Collections.Generic;

namespace CtrlEat.DTOs
{
    /// <summary>
    /// DTOs for Daily Meal Plan Service. Single file containing requests and responses.
    /// </summary>
    public class CreateDailyMealPlanRequest
    {
        public int UserId { get; set; }
        public int? BreakfastRecipeId { get; set; }
        public int? LunchRecipeId { get; set; }
        public int? DinnerRecipeId { get; set; }
        public List<SnackRequest> Snacks { get; set; } = new List<SnackRequest>();
        public DateTime Date { get; set; }
    }

    public class UpdateDailyMealPlanRequest
    {
        public int? BreakfastRecipeId { get; set; }
        public int? LunchRecipeId { get; set; }
        public int? DinnerRecipeId { get; set; }
        public List<SnackRequest> Snacks { get; set; } = new List<SnackRequest>();
        public DateTime? Date { get; set; }
    }

    public class SnackRequest
    {
        public bool IsRecipe { get; set; }
        public int ItemId { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
    }

    public class DailyMealPlanResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? BreakfastRecipeId { get; set; }
        public int? LunchRecipeId { get; set; }
        public int? DinnerRecipeId { get; set; }
        public List<SnackResponse> Snacks { get; set; } = new List<SnackResponse>();
        public DateTime Date { get; set; }
        public decimal TotalCalories { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SnackResponse
    {
        public bool IsRecipe { get; set; }
        public int ItemId { get; set; }
        public string Name { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
        public decimal Calories { get; set; }
    }

    public class GetDailyMealPlansRequest
    {
        public int? Id { get; set; }
        public int? UserId { get; set; }
        public DateTime? Date { get; set; }
    }
}
