using User.Domain.Entities;

namespace User.UnitTests;

public class CalorieGoalTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidUpdatePreservesPreviousGoal(int value)
    {
        var profile = new UserProfile("https://issuer.test", "person-a");
        Assert.Null(profile.CalorieGoal);
        profile.SetCalorieGoal(2100);
        Assert.Throws<ArgumentOutOfRangeException>(() => profile.SetCalorieGoal(value));
        Assert.Equal(2100, profile.CalorieGoal);
    }
}
