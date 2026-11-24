# Issue #25: Profile-picture domain and storage contract

## Accepted images

- Maximum encoded size: 5 MiB (`5 * 1024 * 1024` bytes).
- Allowed content types: `image/jpeg`, `image/png`, and `image/webp`.
- Content must have a matching supported image signature.
- JPEG content must begin with `FF D8` and end with `FF D9`.
- PNG content must begin with the PNG signature and include the `IEND` marker.
- WebP content must use `RIFF`/`WEBP` container signatures.

These checks reject unsupported formats, misleading content types, empty
content, malformed/truncated signatures, and oversized uploads. Signature
checks are a contract baseline; a future image implementation should use full
decoder validation before accepting an upload.

## Domain contract

`ProfilePictureRules` is a pure domain rule:

- `Validate(ProfilePictureUpload)` validates size, content type, and signature.
- `ValidateReference(ProfilePictureReference)` validates storage key, format,
  and size.
- `UserProfile.SetProfilePicture` validates before replacing the saved
  in-memory reference.
- `UserProfile.ClearProfilePicture` removes the reference.

The in-memory reference is unchanged when validation throws.

## Persistent profile contract

`UserProfile.ProfilePicture` is an optional owned EF Core value containing:

- `ProfilePictureStorageKey`
- `ProfilePictureFormat`
- `ProfilePictureSizeBytes`

The EF migration `AddUserProfilePicture` adds these nullable columns to
`Profiles`. Existing calorie-goal profiles remain valid and without an image.

## Shared storage interface

`IProfilePictureStorage` belongs to the Domain and is implemented by Data/API:

- `StoreAsync(accountKey, upload, cancellationToken)`
- `OpenReadAsync(picture, cancellationToken)`
- `DeleteAsync(picture, cancellationToken)`

Implementations must accept an account-bound storage key derived from the
validated issuer and subject. Storage keys, image content, and references must
be private to the service. Implementations must not expose provider credentials
or return an access token in a profile or URL.

## API contract

The profile-picture implementation will use authenticated routes under
`/api/users/me` and the existing `iss`/`sub` identity boundary:

| Operation | Policy | Success | Main failures |
| --- | --- | --- | --- |
| `PUT /api/users/me/profile-picture` | `users.write` | `200`/profile reference | `400`, `401`, `403`, `502` |
| `GET /api/users/me/profile-picture` | `users.read` | `200` private image | `401`, `403`, `404`, `502` |

Exact response bodies and HTTP status choices are frozen by issue #26. An
unsupported or invalid upload returns `400` and does not update the profile or
object-storage set.
