# Issue #27: Milestone 4 security test plan

## Unit tests

- Calorie goal validation preserves the previous saved value.
- Profile-picture format, size, signature, and reference validation.
- Invalid and oversized uploads do not mutate the profile reference.

## Integration tests

The integration suite uses the real ASP.NET Core authentication/authorization
pipeline and API route, with deterministic identity and storage doubles.

Required assertions:

- health is public;
- missing/malformed/expired/wrong issuer/wrong audience/wrong-signature tokens
  return `401`;
- valid read/write scopes are enforced independently;
- valid credentials without a required scope return `403`;
- cross-user `iss`/`sub` identities remain isolated;
- invalid profile updates preserve persisted state;
- allowed profile images persist and read back privately;
- spoofed, unsupported, malformed, and oversized images return an error without
  replacing an existing image;
- storage failures return `502` with a generic body and no secret/provider detail.

## Contract tests

Service-to-service contract coverage treats `IProfilePictureStorage` as the
storage dependency boundary. Deterministic doubles verify:

- compatible `StoreAsync` request data and `ProfilePictureReference` results;
- `OpenReadAsync` success and missing-object results;
- storage exception/error handling with no secret leakage;
- no profile-reference commit after storage failure.

The `IProfilePictureStorage` interface replaces external provider SDK details,
keeping the API contract provider-independent.

## Deterministic identity and storage

JWTs are signed with a test RSA key and configured against a static test
identity-provider discovery document. External object storage is a local
testable implementation or a failing test double. Integration execution remains
at the HTTP/API boundary.

## Execution

The implementation project and test projects target .NET 10. Run:

```bash
dotnet build CtrlEat.slnx
dotnet test CtrlEat.slnx
npm run build
```

This environment currently has only .NET 11, so testhost execution for .NET 10
cannot run until the .NET 10 runtime is installed.
