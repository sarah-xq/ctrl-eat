using System;
using System.Collections.Generic;

namespace CtrlEat.Models
{
    /// <summary>
    /// Domain model for a grocery list.
    /// </summary>
    public class GroceryList
    {
        public int Id { get; set; }
        public List<int> IngredientIds { get; set; } = new List<int>();
        public DateTime Date { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public GroceryList()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
