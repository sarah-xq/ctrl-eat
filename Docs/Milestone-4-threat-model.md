# Milestone 4 Threat Model

## Scope and trust boundaries

The milestone protects the User API and shared profile data. Trust boundaries
exist between:

- the browser/client and the identity provider;
- the browser/client and `User.Api`;
- `User.Api` and User/Data persistence;
- `User.Api` and profile-image/object storage;
- one authenticated account and another account.

The configured identity provider is the authority for authentication. The API
must not treat client-supplied user IDs, names, emails, route parameters, or
secrets as authenticated identity.

## Assets

| Asset | Security goal |
| --- | --- |
| Access and ID tokens | Confidentiality, integrity, short lifetime, local-device access only |
| User profile and calorie goal | Account isolation and persistence integrity |
| Profile image and image reference | Confidentiality, integrity, account isolation, availability |
| Signing keys and provider configuration | Confidentiality and integrity |
| Identity-provider and object-storage secrets | Confidentiality; never in source or browser code |
| Device/session state | Local-only lifetime; independent sign-in/sign-out |

## Attacker capabilities

Potential attackers include unauthenticated callers, authenticated users trying
to access another account, malicious clients that reuse or alter IDs, and
attackers supplying spoofed or malformed image uploads. A compromised browser
may access tokens stored for that browser, but should not be able to copy the
token into another device as a supported handoff mechanism.

## Threats and mitigations

### Authentication

| Threat | Mitigation | Residual risk / verification |
| --- | --- | --- |
| Missing or forged bearer token | ASP.NET Core JWT bearer validation; missing/invalid/malformed/expired/wrong issuer/audience/wrong-signature tokens return `401` | Unit/integration tests verify rejection |
| Wrong identity selected by client | Use validated `iss` and `sub` claims for profile ownership; do not accept user ID or email as identity | Integration tests verify cross-user isolation |
| Replay of a copied token | Short token lifetime, issuer/audience/signature/expiry validation, HTTPS in production, independent device sign-in | Session theft remains possible in a compromised device; provider revocation/logout behavior depends on provider |
| Session persists after sign-out | OIDC redirect sign-out removes local OIDC state; provider end-session behavior is used | Requires manual browser verification and provider configuration |
| Metadata or issuer confusion | Explicit authority/issuer and audience; `RequireHttpsMetadata` is enabled for production | Development loopback is the only documented HTTP exception |

### Authorization

| Operation | Required policy and scope |
| --- | --- |
| `GET /api/users/me` | `users.read` |
| `PUT /api/users/me/calorie-goal` | `users.write` |
| `GET /health` | Public |

Least-privilege scopes are independent. A valid token lacking the required scope
receives `403 Forbidden`. The API must not infer authorization from route IDs,
body identity fields, or email claims.

Cross-user profile access is denied or returns a privacy-preserving `404`, as
documented by the profile API contract. Tests must verify read and update
isolation.

### Profile data

Profile persistence is keyed to the pair `(validated issuer, validated subject)`.
The API must reject missing issuer or subject claims rather than create or
select a profile using fallback identity.

Updates must preserve the prior saved state when domain or persistence
validation fails. A storage failure must not expose provider details or leave
the image reference inconsistent with the stored image set.

### Profile-image upload and storage

Before persistence, an allowed image must pass:

- a documented maximum encoded size;
- an allowed image-format allowlist;
- image parsing/validation rather than MIME type or extension alone;
- account authorization and ownership checks.

Rejected format, misleading content type, malformed content, and oversized
files must not change profile state. Private images remain behind the API
authorization boundary. Any temporary access link must have documented expiry,
least-privilege scope, and no token/profile-secret data in the link or logs.

If object storage is used, provider credentials remain server-side in the
approved secret store. The API must return generic dependency errors and must
not disclose bucket names, keys, account IDs, or secret-storage details.

### Secrets and configuration

Environment-specific values are supplied through configuration and the approved
secret store. Browser environment variables are public configuration only;
they must not contain provider passwords, signing keys, storage credentials, or
client secrets.

The public OIDC client uses authorization code with PKCE. The bearer token is
sent only in the `Authorization` header and is not persisted into profile data,
image URLs, handoff links, or application logs.

### Device and session lifecycle

Device A and Device B must sign in independently. Profile data is shared through
account-bound persistent storage, not by copying an active bearer token or
session. Signing out of a device removes that device's local authenticated state
and requires a new sign-in; it must not delete profile data or another device's
session.

## Dependency failures

Identity-provider discovery or validation failure must fail closed and produce
authentication failure, not a fallback unauthenticated identity. Persistence
and object-storage failures must return a safe error and preserve existing
profile state. Retries or compensation must be designed so an image reference
is not committed when the image write fails, or vice versa.

## Verification matrix

| Area | Required verification |
| --- | --- |
| Authentication | Missing, malformed, expired, wrong issuer, wrong audience, wrong signature, and valid tokens |
| Authorization | Missing scope `403`, valid scope success, cross-user read/update denial |
| Domain validation | Positive-value and future image-format/size validation |
| Integration | API status codes, response contracts, persistence, and dependency failures |
| Contracts | Service/image-storage request and response compatibility and error responses |
| Cross-device | Device A upload, independent Device B sign-in, shared profile retrieval, and cross-user denial |
| Data protection | Private access policy, generic dependency errors, no secret exposure |
| Manual scope | Provider sign-out/independent-device behavior and any unavailable external integration |

Automated tests must assert observable status codes and persisted/retrieved state.
Any external identity-provider or object-storage dependency should use a
deterministic test double. Integration coverage remains at the API boundary.
Manual verification steps must include exact commands, environment values
without secrets, expected results, and evidence location.

## Residual risks and owners

- Identity-provider availability and revocation latency: Authentication owner
  (issue #24).
- Final profile/image storage contract and implementation: profile contract/API
  owners (issues #25 and #26).
- Test completeness: security test owner (issue #27).
- Independent two-device browser verification: cross-device test owner
  (issue #28).
- Secret-store operational controls: deployment owner and issue #24 owner.
- Temporary image links, if introduced, require expiry and access-control
  verification before release.

## Implementation status

Issue #24 authentication and authorization behavior is documented separately in
`Docs/Issue-24-security.md`. Profile-image contract/API implementation issues
#25 and #26 remain open, so this threat model records their required controls
rather than claiming that upload implementation or cross-device evidence already
exists.
