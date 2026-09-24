using System;
using System.Collections.Generic;

namespace CtrlEat.DTOs
{
    // ============ SHARED INGREDIENT CONTRACT ============

    /// <summary>
    /// Ingredient information returned to the Storage and Grocery services.
    /// </summary>
    public class StoredIngredientResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FoodGroup { get; set; }
        public decimal Calories { get; set; }
    }

    // ============ STORAGE LOCATION SERVICE CONTRACTS ============

    public class CreateStorageLocationRequest
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public decimal MinimumTemperature { get; set; }
        public decimal MaximumTemperature { get; set; }
        public List<int> IngredientIds { get; set; } = new List<int>();
    }

    public class StorageLocationResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public decimal MinimumTemperature { get; set; }
        public decimal MaximumTemperature { get; set; }
        public List<StoredIngredientResponse> Ingredients { get; set; } = new List<StoredIngredientResponse>();
        public DateTime CreatedAt { get; set; }
    }

    public class GetStorageLocationsRequest
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }

    // ============ GROCERY LIST SERVICE CONTRACTS ============

    public class CreateGroceryListRequest
    {
        public List<int> IngredientIds { get; set; } = new List<int>();
        public DateTime Date { get; set; }
    }

    public class GroceryListResponse
    {
        public int Id { get; set; }
        public List<StoredIngredientResponse> Ingredients { get; set; } = new List<StoredIngredientResponse>();
        public DateTime Date { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class GetGroceryListsRequest
    {
        public int? Id { get; set; }
        public DateTime? Date { get; set; }
    }
}
