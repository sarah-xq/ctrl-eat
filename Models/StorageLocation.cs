using System;
using System.Collections.Generic;

namespace CtrlEat.Models
{
    /// <summary>
    /// Domain model for an ingredient storage location.
    /// </summary>
    public class StorageLocation
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public decimal MinimumTemperature { get; set; }
        public decimal MaximumTemperature { get; set; }
        public List<int> IngredientIds { get; set; } = new List<int>();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public StorageLocation()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
