using System;

namespace CtrlEat.Models
{
	/// <summary>
	/// Domain model for Ingredient
	/// Represents the core entity in the Ingredient Service
	/// </summary>
	public class Ingredient
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string FoodGroup { get; set; }
		public DateTime? ExpiryDate { get; set; }
		public decimal Calories { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }

		public Ingredient()
		{
			CreatedAt = DateTime.UtcNow;
			UpdatedAt = DateTime.UtcNow;
		}
	}
}
