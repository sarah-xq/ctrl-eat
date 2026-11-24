namespace User.Domain.Entities;

public enum ProfilePictureFormat
{
    Jpeg,
    Png,
    WebP
}

public sealed record ProfilePictureReference(
    string StorageKey,
    ProfilePictureFormat Format,
    long SizeBytes);

public sealed record ProfilePictureUpload(
    string ContentType,
    byte[] Content);

public interface IProfilePictureStorage
{
    Task<ProfilePictureReference> StoreAsync(
        string accountKey,
        ProfilePictureUpload upload,
        CancellationToken cancellationToken);

    Task<Stream?> OpenReadAsync(
        ProfilePictureReference picture,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        ProfilePictureReference picture,
        CancellationToken cancellationToken);
}

public static class ProfilePictureRules
{
    public const long MaximumSizeBytes = 5 * 1024 * 1024;

    public static ProfilePictureFormat Validate(ProfilePictureUpload upload)
    {
        ArgumentNullException.ThrowIfNull(upload);
        ArgumentNullException.ThrowIfNull(upload.Content);

        if (upload.Content.Length == 0 || upload.Content.LongLength > MaximumSizeBytes)
        {
            throw new ArgumentException(
                $"Profile pictures must contain 1-{MaximumSizeBytes} bytes.",
                nameof(upload));
        }

        var format = UploadFormat(upload.ContentType);
        if (!HasMatchingImageSignature(upload.Content, format))
        {
            throw new ArgumentException(
                "The content is not a complete, supported profile image.",
                nameof(upload));
        }

        return format;
    }

    public static void ValidateReference(ProfilePictureReference picture)
    {
        ArgumentNullException.ThrowIfNull(picture);

        if (string.IsNullOrWhiteSpace(picture.StorageKey))
        {
            throw new ArgumentException("A profile-picture storage key is required.", nameof(picture));
        }

        if (picture.SizeBytes is <= 0 or > MaximumSizeBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(picture),
                $"Profile picture size must be 1-{MaximumSizeBytes} bytes.");
        }

        if (!Enum.IsDefined(picture.Format))
        {
            throw new ArgumentOutOfRangeException(
                nameof(picture),
                "Unsupported profile-picture format.");
        }
    }

    public static ProfilePictureFormat UploadFormat(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("An image content type is required.", nameof(contentType));
        }

        var normalized = contentType.Split(';', 2)[0].Trim().ToLowerInvariant();
        return normalized switch
        {
            "image/jpeg" => ProfilePictureFormat.Jpeg,
            "image/png" => ProfilePictureFormat.Png,
            "image/webp" => ProfilePictureFormat.WebP,
            _ => throw new ArgumentException(
                "Only JPEG, PNG, and WebP profile pictures are supported.",
                nameof(contentType))
        };
    }

    private static bool HasMatchingImageSignature(byte[] content, ProfilePictureFormat format)
    {
        return format switch
        {
            ProfilePictureFormat.Jpeg =>
                content.Length >= 4
                && content[0] == 0xFF
                && content[1] == 0xD8
                && content[^2] == 0xFF
                && content[^1] == 0xD9,
            ProfilePictureFormat.Png =>
                content.Length >= 12
                && content.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
                && content.AsSpan().IndexOf("IEND"u8) >= 0,
            ProfilePictureFormat.WebP =>
                content.Length >= 12
                && content.AsSpan(0, 4).SequenceEqual("RIFF"u8)
                && content.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
    }
}
