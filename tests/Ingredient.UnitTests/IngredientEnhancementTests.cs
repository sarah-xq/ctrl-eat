using Ingredient.Domain;
using Xunit;

public sealed class IngredientEnhancementTests
{
    [Theory]
    [InlineData(-2, ExpiryStatus.Expired)]
    [InlineData(0, ExpiryStatus.SoonExpiring)]
    [InlineData(5, ExpiryStatus.SoonExpiring)]
    [InlineData(10, ExpiryStatus.Fresh)]
    public void Expiry_status_tracks_expiry_threshold(int relativeDays, ExpiryStatus expected)
    {
        var today = new DateTime(2026, 11, 11, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(expected, IngredientRules.GetExpiryStatus(today.AddDays(relativeDays), 7, today));
    }

    [Fact]
    public void No_expiry_date_has_no_alert() =>
        Assert.Equal(ExpiryStatus.NoDate, IngredientRules.GetExpiryStatus(null, 7, DateTime.UtcNow));

    [Fact]
    public void Negative_nutrition_rejected() =>
        Assert.Throws<ArgumentException>(() => new NutritionalInfo { SodiumMilligrams = -1 }.Validate());

    [Fact]
    public void Invalid_storage_method_rejected() =>
        Assert.Throws<ArgumentException>(() => IngredientRules.ValidateStorage("Shed", null, 7));
}
