using Xunit;
using Recipe.Domain;
public class RecipeTests
{
    [Fact] public void RejectsInvalidTimes() => Assert.Throws<ArgumentException>(() => Recipe.Domain.Recipe.Validate("Meal", 10, 20, new[] {(1,1m)}));
    [Fact] public void RejectsDuplicateIngredient() => Assert.Throws<ArgumentException>(() => Recipe.Domain.Recipe.Validate("Meal", 30, 10, new[] {(1,1m),(1,2m)}));
    [Fact] public void AcceptsValidRecipe() => Recipe.Domain.Recipe.Validate("Meal", 30, 10, new[] {(1,1m)});
}
