namespace User.Domain.Entities;

public class UserProfile
{
    public int Id { get; set; }

    public string Issuer { get; }

    public string Subject { get; }

    public int? CalorieGoal { get; private set; }

    public UserProfile(string issuer, string subject)
    {
        Issuer = issuer;
        Subject = subject;
    }

    public void SetCalorieGoal(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Calorie goal must be greater than zero.");
        }

        CalorieGoal = value;
    }
}