# Issue #26: Profile-picture API and persistence

## API contract

| Operation | Policy | Success | Failure |
| --- | --- | --- | --- |
| `PUT /api/users/me/profile-picture` multipart field `file` | `users.write` | `200` profile-picture metadata | `400`, `401`, `403`, `502` |
| `GET /api/users/me/profile-picture` | `users.read` | `200` private image bytes | `401`, `403`, `404`, `502` |

The API derives account identity exclusively from validated `iss` and `sub`
claims. It does not accept a route ID or client identity field.

## Upload boundary

The API rejects missing/empty files and files over 5 MiB before storage. Domain
validation also checks content type and image signatures. Invalid uploads do not
change the profile reference or replace the saved object.

## Storage boundary

The Data-layer local storage implementation is a private server-side
object-storage adapter:

- account key is an issuer/subject pair;
- storage name is a SHA-256 account hash plus allowed extension;
- object bytes remain in server-side `profile-pictures/` storage;
- no access token or temporary URL is returned;
- private image retrieval goes through the authorized API route.

The same pattern can be replaced by an approved object-storage implementation
without changing API identity or authorization rules.

## Atomicity and failure handling

The service validates before writing, writes the object, saves the profile
reference, and cleans up an uncommitted object on profile-save failure. If a
previous object is replaced, deletion of the previous object is best-effort
cleanup; storage/provider failures return a generic `502` and do not expose
credentials or provider details.
