# Issue #12 · Visual Studio Copilot Planner

[Work item](https://github.com/sarah-xq/ctrl-eat/issues/12) · Owner: Louis-FelixRDC · Source revision: `62e64e89`

**Status: Generated in Visual Studio. Human plan review completed on November 5, 2026.** The repository agent was **CtrlEat Planner**. Visual Studio reported that `code_search` and `find_references` were unavailable. Its chat recorded successful reads of the planning skill, User.cs, UsersController.cs, UserService.cs, CtrlEat.csproj and Program.cs. The test baseline was supplied in the prompt.

## Copilot response

### Gate 1 plan — Issue #12, owner: Louis-FelixRDC

1. **Freeze contract and acceptance criteria**
   - Signed-in users can read/update only their own goal.
   - `GET /api/users/me` requires `users.read`; returns `200` with `UserMeResponse { id, name, email, calorieGoal: int? }`. Initial goal is `null`.
   - `PUT /api/users/me/calorie-goal` requires `users.write`; request DTO is `UpdateCalorieGoalRequest { calorieGoal: int }`. Positive integers return `200` and the saved value.
   - Missing/invalid token returns `401`; valid token without the required scope returns `403`.
   - Null, zero, negative, non-integer, malformed, or missing input returns `400`, leaves the previous value unchanged, and displays the server error in the client.
   - Values survive reload, API restart, and database restart.
2. **Domain — Domain agent**
   - Update `Models/User.cs` so `CalorieGoal` is nullable (`int?`) and enforce the positive-value rule in a domain/service validation object or method.
   - Preserve numeric `User.Id`, because meal plans reference it.
   - Add/update `DTOs/UserMeResponse.cs` and `DTOs/UpdateCalorieGoalRequest.cs` if DTOs remain in the current project; otherwise place them in the API contract layer.
3. **Data — EF Core/Data agent**
   - Add `Data/AppDbContext.cs`, `Data/Configurations/UserConfiguration.cs`, and an EF repository/service replacing `JsonUserRepository` for users and meal-plan ownership queries.
   - Create a migration such as `AddNullableUserCalorieGoal`; convert existing zero/default goals to `NULL` unless the human chooses another data policy.
   - Preserve numeric IDs and establish foreign keys/ownership indexes needed for isolation.
   - Update `Program.cs` registration and root `CtrlEat.csproj` package/item globs so test sources are not compiled into the API.
4. **API/auth — API agent**
   - Add `Controllers/UsersController.cs` actions for the two `/me` routes and a focused `UserCalorieGoalService.cs`; derive identity from claims, never route-supplied IDs.
   - Configure OIDC bearer validation, `users.read`/`users.write` policies, and middleware ordering (`UseAuthentication` before authorization).
   - Support the existing local password flow only through an explicitly approved compatibility boundary; do not infer authorization from email alone.
   - Independently launch the API and document its authority/base URL for the client.
5. **Client and cutover — Client agent**
   - Add/update client API/auth files and the profile/settings screen to use REST, OIDC authorization-code + PKCE, scope requests, loading state, null initial state, validation errors, and persisted success.
   - Cut over all callers from legacy `/api/users/{id}` profile/update routes; deprecate/remove them or make them non-authoritative and deny cross-user access.
6. **Tests and checkpoints — Test/validation agent**
   - Unit tests: positive/null validation and unchanged-on-failure behavior.
   - Integration tests: migration, reload/restart persistence, numeric meal-plan ownership, cross-user isolation, `401`, `403`, malformed requests, and both endpoints.
   - Client tests: initial null, successful persistence, and visible `400` error.
   - Baseline command: `dotnet build CtrlEat.slnx`; `dotnet test CtrlEat.slnx` (baseline: 11 meal-plan tests passed, 73 warnings).

**Human decisions before implementation:** approve DTO/HTTP contract, JSON-to-EF migration and zero-to-null policy, OIDC issuer/audience and scope claims, local-password compatibility, legacy-route removal timing, and independent API launch configuration.

## Codex review notes for Félix

- Put the entity in `src/User.Domain`, EF Core in `src/User.Data`, and contracts and routes in an independently runnable `src/User.Api`. The old root-project paths above describe the starting code. Exclude nested source, content and tests from the legacy project's default globs as appropriate.
- Keep the numeric user ID for existing references. Link profiles to a unique validated issuer and subject. Keep meal-plan ownership checks in the MealPlan service and use public contracts at service boundaries.
- For issue #12, the screen needs only `{ "calorieGoal": null }` or a saved integer. Name and email can stay outside this response. Let the identity provider handle sign-in with PKCE. Protect or retire legacy User routes as part of cutover.
- The tracked `Data/users.json` contains an empty array. Check for deployed legacy data before applying any import or conversion rule. Agree on the team's provider settings and client stack before integration.

## Gate 1 decision

Louis-FelixRDC approved the plan with two changes: separate User.Domain, User.Data and User.Api projects, and a goal-only response keyed to validated identity. The developer authorized a local demo stack. Implementation and peer review are separate gates.

