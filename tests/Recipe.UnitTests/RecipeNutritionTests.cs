using Recipe.Domain;
using Xunit;

public sealed class RecipeNutritionTests
{
    [Fact]
    public void Nutrition_totals_scale_by_ingredient_quantity()
    {
        var running = new NutritionAmounts();
        running = running.PlusScaled(new NutritionAmounts(2, 3, 4, 1, 2, 50, 10), 3);
        Assert.Equal(6, running.ProteinGrams);
        Assert.Equal(9, running.CarbohydratesGrams);
        Assert.Equal(150, running.SodiumMilligrams);
    }
}
