using DailyMealPlan.Domain.Models;

namespace DailyMealPlan.UnitTests;

public class CalorieTests
{
    [Fact]
    public void MealsAndFractionalSnacksAreCountedIndependently()
    {
        var plan = new MealPlan("issuer", "subject", new DateOnly(2026, 11, 5))
        {
            BreakfastRecipeId = 1, LunchRecipeId = 1, DinnerRecipeId = 1,
            Snacks = [new(true, 1, 0.5m), new(false, 2, 2m)]
        };
        plan.RecalculateTotalCalories(new Dictionary<int, decimal> { [1] = 40m },
            new Dictionary<int, decimal> { [2] = 40m });
        Assert.Equal(220m, plan.TotalCalories);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SnackRequiresPositiveQuantity(int quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlanSnack(false, 1, quantity));
    }
}
