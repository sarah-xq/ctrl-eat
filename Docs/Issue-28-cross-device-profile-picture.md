# Issue #28: Cross-device profile-picture verification

## Independent sessions

Each browser/device starts the configured OIDC authorization-code flow with
PKCE. The access token is never placed in profile data, image URLs, handoff
links, or storage references.

The automated API-level workflow creates two separate `HttpClient` instances:

1. Device A receives an independently issued test token for the same subject.
2. Device A uploads an allowed JPEG to `/api/users/me/profile-picture`.
3. Device B receives a separately issued test token for the same subject.
4. Device B retrieves `/api/users/me/profile-picture` and sees the shared image.
5. A different subject receives `404` for private read and cannot replace the
   original account image.
6. Both the original and the other account retain independent profile state.

The test asserts `200` upload/read, persisted image bytes, independent
`HttpClient` instances, and cross-user isolation. It does not copy Device A's
token or session into Device B.

## Manual browser verification

### Prerequisites

1. Start the identity provider:

   ```bash
   docker compose -f dev/user-identity/compose.yaml up
   ```

2. Start the API and client:

   ```bash
   dotnet run --project src/User.Api
   cd src/User.Client && npm ci && npm run dev
   ```

No secret values are required in this procedure; use disposable local accounts.

### Device A

1. Open `http://localhost:5173` on Device A.
2. Sign in independently.
3. Upload an allowed 5 MiB or smaller JPEG/PNG/WebP through the profile UI.
4. Confirm the API returns `200`.
5. Record the account subject only as test metadata; do not record bearer tokens.

### Device B

1. Open the client in a separate browser profile/device.
2. Sign in independently as the same account.
3. Request the profile picture through the private API route.
4. Confirm the same uploaded bytes are displayed and no Device A token/session
   was copied.

### Isolation and sign-out

1. Sign in as another account and try to read/replace the first account picture.
   Expect `404` or `403` according to the privacy policy.
2. Sign out on Device A and/or Device B.
3. Confirm the signed-out browser clears OIDC state and protected API requests
   return `401`.
4. Confirm the profile picture remains available to the account when a new
   independent session signs in.

## Evidence record

Record for each browser/API run:

- timestamp and environment;
- account labels only, not provider passwords or tokens;
- request route and HTTP status;
- response image hash or byte length;
- sign-out result;
- screenshot or API log location.
