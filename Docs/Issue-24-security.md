# Issue #24: Authentication, authorization, and secret configuration

## Trust boundary

`User.Api` is a bearer-token resource server. The configured identity provider is
the authority of identity; the API does not accept a user ID, email address, or
other client-supplied identity selector for `/api/users/me` operations.

The JWT bearer handler validates:

- the configured issuer (`Authentication:Authority`);
- the configured audience (`Authentication:Audience`);
- the provider's signing key and the required signed-token policy; and
- token lifetime with zero clock skew.

Tokens must contain both `iss` and `sub`. A missing, malformed, expired, wrong
issuer, wrong audience, or incorrectly signed token receives `401 Unauthorized`.

## Authorization matrix

| Operation | Policy | Required scope |
| --- | --- | --- |
| `GET /api/users/me` | `users.read` | `users.read` |
| `PUT /api/users/me/calorie-goal` | `users.write` | `users.write` |
| `GET /health` | Public | None |

A valid token without the required scope receives `403 Forbidden`. Identity is
read from validated `iss` and `sub` claims only.

## Configuration and secrets

Development defaults are isolated in `src/User.Api/appsettings.Development.json`.
Production deployments must provide the following through environment-specific
configuration and the approved secret store:

```text
Authentication__Authority
Authentication__Audience
Authentication__RequireHttpsMetadata=true
ConnectionStrings__User
Cors__ClientOrigin
```

No client secret, provider password, signing key, access token, or secret-store
credential belongs in source control or browser code. The React client uses
authorization code with PKCE as a public client and keeps its OIDC state in
session storage; it does not contain a client secret.

## Sign-out and device access

Each device performs its own OIDC sign-in. A device's local authenticated state
is not copied to another device. Client sign-out uses the provider logout
redirect and removes the local OIDC user state; subsequent protected API calls
must be authenticated again.

## Automated coverage

`tests/User.IntegrationTests/UserProfileApiTests.cs` covers the public health
endpoint, missing credentials, malformed/expired/wrong issuer/wrong audience/
wrong-signature tokens, missing subject claims, independent read/write scopes,
invalid input, persistence across API hosts, and cross-user isolation.
