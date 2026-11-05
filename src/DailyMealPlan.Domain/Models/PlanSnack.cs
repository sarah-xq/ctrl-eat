namespace DailyMealPlan.Domain.Models;

public sealed class PlanSnack
{
    private decimal _quantity = 1m;

    public int Id { get; set; }
    public bool IsRecipe { get; set; }
    public int ItemId { get; set; }
    public string? Name { get; set; }
    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Quantity must be greater than zero.");
            }

            _quantity = value;
        }
    }

    public string? Unit { get; set; }
    public decimal Calories { get; set; }

    public PlanSnack()
    {
    }

    public PlanSnack(bool isRecipe, int itemId, decimal quantity, string? unit = null, string? name = null)
    {
        IsRecipe = isRecipe;
        ItemId = itemId;
        Quantity = quantity;
        Unit = unit;
        Name = name;
    }

    public decimal CalculateCalories(decimal caloriesPerUnit)
    {
        return caloriesPerUnit * Quantity;
    }
}