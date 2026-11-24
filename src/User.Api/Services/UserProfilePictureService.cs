using Microsoft.Extensions.Logging;
using User.Data;
using User.Domain.Entities;

namespace User.Api.Services;

public sealed class UserProfilePictureService
{
    private readonly UserProfileRepository repository;
    private readonly IProfilePictureStorage storage;
    private readonly ILogger<UserProfilePictureService> logger;

    public UserProfilePictureService(
        UserProfileRepository repository,
        IProfilePictureStorage storage,
        ILogger<UserProfilePictureService> logger)
    {
        this.repository = repository;
        this.storage = storage;
        this.logger = logger;
    }

    public async Task<ProfilePictureReference?> ReadAsync(
        string issuer,
        string subject,
        CancellationToken cancellationToken)
    {
        var profile = await repository.ReadGoalAsync(issuer, subject, cancellationToken);
        return profile?.ProfilePicture;
    }

    public async Task<ProfilePictureReference> ReplaceAsync(
        string issuer,
        string subject,
        ProfilePictureUpload upload,
        CancellationToken cancellationToken)
    {
        ProfilePictureRules.Validate(upload);
        var accountKey = $"{issuer}\n{subject}";
        var profile = await repository.ReadGoalAsync(issuer, subject, cancellationToken)
            ?? new UserProfile(issuer, subject);
        var previous = profile.ProfilePicture;
        ProfilePictureReference stored;

        try
        {
            stored = await storage.StoreAsync(accountKey, upload, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning("Profile picture storage write failed.");
            throw new ProfilePictureStorageException("Profile picture storage is unavailable.", exception);
        }

        try
        {
            profile.SetProfilePicture(stored);
            await repository.SaveProfilePictureAsync(profile, cancellationToken);
        }
        catch
        {
            try
            {
                await storage.DeleteAsync(stored, cancellationToken);
            }
            catch (Exception cleanupException)
            {
                logger.LogWarning(cleanupException, "Could not clean up an uncommitted profile picture.");
            }

            throw;
        }

        if (previous is not null
            && previous.StorageKey != stored.StorageKey)
        {
            try
            {
                await storage.DeleteAsync(previous, cancellationToken);
            }
            catch (Exception cleanupException)
            {
                logger.LogWarning(cleanupException, "Could not remove the previous profile picture.");
            }
        }

        return stored;
    }

    public Task<Stream?> OpenReadAsync(
        ProfilePictureReference picture,
        CancellationToken cancellationToken)
    {
        return storage.OpenReadAsync(picture, cancellationToken);
    }
}

public sealed class ProfilePictureStorageException : Exception
{
    public ProfilePictureStorageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
