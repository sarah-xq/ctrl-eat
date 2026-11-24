using System.Security.Cryptography;
using System.Text;
using User.Domain.Entities;

namespace User.Data;

public sealed class LocalProfilePictureStorage : IProfilePictureStorage
{
    private readonly string rootPath;

    public LocalProfilePictureStorage(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Profile picture storage path is required.", nameof(rootPath));
        }

        this.rootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(this.rootPath);
    }

    public async Task<ProfilePictureReference> StoreAsync(
        string accountKey,
        ProfilePictureUpload upload,
        CancellationToken cancellationToken)
    {
        var format = ProfilePictureRules.Validate(upload);
        var extension = format switch
        {
            ProfilePictureFormat.Jpeg => "jpg",
            ProfilePictureFormat.Png => "png",
            ProfilePictureFormat.WebP => "webp",
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
        var accountHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(accountKey))).ToLowerInvariant();
        var objectName = $"{accountHash}.{extension}";
        var filePath = Path.Combine(rootPath, objectName);
        await using var stream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous);
        await stream.WriteAsync(upload.Content, cancellationToken);
        await stream.FlushAsync(cancellationToken);

        return new ProfilePictureReference(objectName, format, upload.Content.LongLength);
    }

    public Task<Stream?> OpenReadAsync(
        ProfilePictureReference picture,
        CancellationToken cancellationToken)
    {
        ProfilePictureRules.ValidateReference(picture);
        if (!IsSafeObjectName(picture.StorageKey))
        {
            throw new InvalidOperationException("Invalid profile picture storage key.");
        }

        var filePath = Path.Combine(rootPath, picture.StorageKey);
        Stream? stream = File.Exists(filePath)
            ? new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)
            : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(
        ProfilePictureReference picture,
        CancellationToken cancellationToken)
    {
        ProfilePictureRules.ValidateReference(picture);
        if (!IsSafeObjectName(picture.StorageKey))
        {
            throw new InvalidOperationException("Invalid profile picture storage key.");
        }

        var filePath = Path.Combine(rootPath, picture.StorageKey);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    private static bool IsSafeObjectName(string storageKey)
    {
        var supportedExtension = storageKey.EndsWith(".jpg", StringComparison.Ordinal)
            || storageKey.EndsWith(".png", StringComparison.Ordinal)
            || storageKey.EndsWith(".webp", StringComparison.Ordinal);
        if (!supportedExtension)
        {
            return false;
        }

        var hashLength = storageKey.LastIndexOf('.');
        if (hashLength != 64)
        {
            return false;
        }

        return storageKey[..hashLength].All(char.IsAsciiHexDigitLower)
            && storageKey[hashLength] == '.';
    }
}
