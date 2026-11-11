using Xunit;
using Ingredient.Domain;
public class IngredientTests
{
    [Fact] public void RejectsNegativeCalories() => Assert.Throws<ArgumentException>(() => Ingredient.Domain.Ingredient.Validate("Rice", "Grain", -1));
    [Fact] public void RejectsBlankName() => Assert.Throws<ArgumentException>(() => Ingredient.Domain.Ingredient.Validate(" ", "Grain", 100));
    [Fact] public void AcceptsValidIngredient() => Ingredient.Domain.Ingredient.Validate("Rice", "Grain", 100);
}
