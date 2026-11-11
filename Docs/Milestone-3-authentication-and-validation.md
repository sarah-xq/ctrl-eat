# Milestone 3 — Authentication, Authorization and Validation Evidence

Evidence snapshot: November 10, 2026. This document records configurations and prior results; it does **not** claim fresh tests on an unresolved merged tree.

## OAuth2/OIDC architecture

- Resource owner: the signed-in CtrlEat account holder.
- Authorization server: Keycloak in the local development demo (`http://localhost:8082/realms/ctrl-eat`).
- Clients: React/Vite `User.Client` (`ctrleat-client`) and ASP.NET Core Razor Pages `DailyMealPlan.Client` (`ctrleat-mealplans-client`), with authorization-code plus PKCE. Zachary's Razor frontend also documents PKCE in `src/CtrlEat.RazorClient/OIDC-SETUP.md`; confirm provider registration separately.
- Resource servers: standalone User, DailyMealPlan, StorageLocation and GroceryList ASP.NET Core APIs.
- API JWT bearer configuration validates signing key/signature, issuer, audience and lifetime, requires `iss` and `sub`, and enforces named policies with explicit scopes. User and DailyMealPlan map account ownership from validated issuer/subject rather than caller-supplied IDs.
- User API audience: `ctrleat-users`; Daily Meal Plan audience: `ctrleat-mealplans`. Storage and Grocery audience values depend on `Authentication:Audience` configuration and require owner confirmation.
- Keep secrets outside source control; production uses HTTPS and provider configuration managed for the deployment.

## Protected operations

| Service | Public endpoint | Read operations / policy | Write operations / policy |
| --- | --- | --- | --- |
| User | `GET /health` | `GET /api/users/me` — `users.read` | `PUT /api/users/me/calorie-goal` — `users.write` |
| DailyMealPlan | `GET /health` | `GET /api/daily-meal-plans`, `GET /api/daily-meal-plans/{id}` — `daily-meal-plans.read` | `POST /api/daily-meal-plans`, `PUT /api/daily-meal-plans/{id}`, `DELETE /api/daily-meal-plans/{id}` — `daily-meal-plans.write` |
| StorageLocation | `GET /health` | `GET /api/StorageLocations[/{id}]` — `storage-locations.read` | `POST`, `PUT`, `DELETE /api/StorageLocations[/{id}]` — `storage-locations.write` |
| GroceryList | `GET /health` | `GET /api/GroceryLists[/{id}]` — `grocery-lists.read` | `POST`, `PUT`, `DELETE /api/GroceryLists[/{id}]` — `grocery-lists.write` |
| Ingredient and Recipe | **Unverified** | **Unverified** | **Unverified** |

For StorageLocation/GroceryList, actual route templates use `[Route("api/[controller]")]`. Confirm downstream Ingredient service authentication before deploying.

## Required security validation

For **each** service, record actual test results for: no token → 401; expired/invalid/wrong-issuer/wrong-audience token → rejection; valid token with insufficient scope → 403; valid token with required scope → allowed; read scope does not authorize write; public endpoint responds without token. Confirm ownership boundaries and cross-user isolation where applicable.

- **User:** `Docs/Issue-12-demo.md` and [PR #13](https://github.com/sarah-xq/ctrl-eat/pull/13) record a successful local auth/browser run and 16 tests (11 existing and 5 additional). Revalidate on the final combined branch.
- **Daily Meal Plan:** [PR #16 Validator comment](https://github.com/sarah-xq/ctrl-eat/pull/16#issuecomment-6692620471) records 0 warnings/errors and **21 passing .NET tests** for head `af17fbd` compared to base `a54f2fb`. Chromium demo covered sign-in, plan persistence and editing, invalid reference retention and deletion. Ownership/outage coverage was in API tests, not repeated by Validator in a browser. These results do not validate later merge conflicts/resolution.
- **StorageLocation and GroceryList:** merged [PR #18](https://github.com/sarah-xq/ctrl-eat/pull/18) contains implementation and test projects. Exact security matrix outcomes and reviewer signoffs need confirmation.
- **Ingredient/Recipe:** standalone Milestone 3 API policies, client OIDC flow and test results not verified.

## Repeat on integrated branch

```bash
dotnet build CtrlEat.slnx -warnaserror -m:1
dotnet test CtrlEat.slnx --no-build -m:1
```

Build relevant frontend projects using their documented Node/npm commands. Record commit SHA, test run date/environment, errors/warnings, security scenarios, any fixes, and real human reviewer approvals. Never mark author or peer signoff complete without that person's confirmation.

## Integration verification after resolving PR #16 merge conflicts

The author reported the following *locally executed* checks on branch `issue-14-daily-meal-plans`, after integrating the latest `feature/milestone-3-layered-service-development` changes. The conflict in `CtrlEat.slnx` was resolved by preserving projects from both branches; 27 projects were listed in the combined solution.

- `git diff --cached --check` — no whitespace errors reported.
- `git ls-files -u` — no unmerged paths reported.
- `dotnet build CtrlEat.slnx -warnaserror -m:1` — succeeded (the 27 projects compiled with warnings treated as errors).
- `dotnet test CtrlEat.slnx --no-build -m:1` — **169 total, 169 passed, 0 failed, 0 skipped**, as reported in the terminal output.
- Merge commit **`24d9ae5`** was pushed to `origin/issue-14-daily-meal-plans`; `git status -sb` showed a clean branch tracking the remote.

These are meaningful **integrated .NET build and automated test results**, but do not replace browser-based checks for every frontend, the author's independent diff/security review, teammate approval of the integrated revision, or verification of independent Ingredient and Recipe services. This information was supplied by the person executing the terminal commands; it is not represented as a separate CI run or independently repeated by the documentation preparer.
