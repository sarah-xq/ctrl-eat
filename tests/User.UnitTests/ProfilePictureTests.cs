using User.Domain.Entities;

namespace User.UnitTests;

public class ProfilePictureTests
{
    [Theory]
    [InlineData("image/gif")]
    [InlineData("text/plain")]
    [InlineData("image/jpeg")]
    public void UnsupportedAndSpoofedContentAreRejected(string contentType)
    {
        var content = contentType == "image/jpeg"
            ? new byte[] { 0xFF, 0xD8, 0x00, 0xD9 }
            : "GIF89a".Select(character => (byte)character).ToArray();
        var profile = new UserProfile("https://issuer.test", "person-a");
        var previous = new ProfilePictureReference(
            "profiles/person-a/current.png",
            ProfilePictureFormat.Png,
            100);
        profile.SetProfilePicture(previous);

        Assert.ThrowsAny<ArgumentException>(() =>
            ProfilePictureRules.Validate(new ProfilePictureUpload(contentType, content)));
        Assert.Same(previous, profile.ProfilePicture);
    }

    [Fact]
    public void OversizedContentIsRejectedWithoutChangingProfile()
    {
        var profile = new UserProfile("https://issuer.test", "person-a");
        var previous = new ProfilePictureReference(
            "profiles/person-a/current.webp",
            ProfilePictureFormat.WebP,
            100);
        profile.SetProfilePicture(previous);
        var content = new byte[ProfilePictureRules.MaximumSizeBytes + 1];

        Assert.Throws<ArgumentException>(() =>
            ProfilePictureRules.Validate(new ProfilePictureUpload("image/webp", content)));
        Assert.Same(previous, profile.ProfilePicture);
    }

    [Fact]
    public void ValidSignaturesProduceDocumentedFormats()
    {
        Assert.Equal(
            ProfilePictureFormat.Jpeg,
            ProfilePictureRules.Validate(new ProfilePictureUpload(
                "image/jpeg",
                [0xFF, 0xD8, 0x01, 0x02, 0xFF, 0xD9])));

        Assert.Equal(
            ProfilePictureFormat.Png,
            ProfilePictureRules.Validate(new ProfilePictureUpload(
                "image/png",
                [137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0, 73, 69, 78, 68])));

        Assert.Equal(
            ProfilePictureFormat.WebP,
            ProfilePictureRules.Validate(new ProfilePictureUpload(
                "image/webp",
                [(byte)'R', (byte)'I', (byte)'F', (byte)'F',
                 4, 0, 0, 0, (byte)'W', (byte)'E', (byte)'B', (byte)'P'])));
    }
}
