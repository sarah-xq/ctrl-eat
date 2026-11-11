# Milestone 3 follow-up — implementation and required human gates

**Work item:** #19 Ingredient nutrition/expiry/allergen enhancements; Ingredient/Recipe refactor and downstream API integration.
**Branch:** `feature/ingredient-recipe-layered-m3` (draft PR #22).
**Status:** implementation candidate. Do not mark merged, reviewed, 85%-covered, or ready for Lea until the checks below have actually happened.

## Research / plan

- Preserve existing public URLs (`/api/Ingredients`, `/api/recipes`), ID and calorie fields consumed by Daily Meal Plan, Grocery List and Storage Location.
- Keep Domain pure, persistence in Data/repositories, thin route handlers plus DTOs in API, browser clients using HTTP only.
- Expand Ingredient with macro/micro nutrition, serving reference, data source/date, expiry storage and batch metadata, configurable warning thresholds, named allergens (including custom names) and Contains/MayContain/Trace presence.
- Use additive EF Core migrations. Existing records remain in place, with new nullable/default fields. Existing calorie values are not silently migrated to a new nutritional reference system.
- Make Recipe compute nutrition and union allergen presence from Ingredient HTTP responses when creating/updating a recipe. Recipe stores nutritional snapshots, so future changes to an Ingredient do **not** retroactively modify an existing Recipe; refresh by editing/saving the Recipe.
- Forward the **calling user's validated bearer token** for read-only downstream lookups. No service token or authentication bypass is added. Configure clients with Ingredient/Recipe audiences and read scopes. In-memory `DevelopmentIngredientHandler` stubs run only in **Testing**, not interactive Development.

## API and permission mapping

| Endpoint | Policy | Notes |
| --- | --- | --- |
| `GET /api/Ingredients`, `GET /api/Ingredients/{id}` | `ingredients.read` | Optional `allergen` query filter on list; returns public DTOs |
| `GET /api/Ingredients/alerts` | `ingredients.read` | On-demand list of expired/soon-expiring entries; **not** a background push notification |
| `POST/PUT/DELETE /api/Ingredients` | `ingredients.write` | Validates nutrition, storage, allergens; migration preserves existing rows |
| `GET /api/recipes`, `GET /api/recipes/{id}` | `recipes.read` | Returns public DTOs including nutrition and allergens |
| `POST/PUT/DELETE /api/recipes` | `recipes.write` | For Ingredient lookups, token also needs `ingredients.read` and the Ingredient audience |
| `GET /health` on both APIs | Public | Anonymous only by design |

## Automated checks to collect (do not assert before running)

```powershell
dotnet build CtrlEat.slnx -warnaserror -m:1
dotnet test CtrlEat.slnx --no-build -m:1
```

Added unit and signed JWT / SQLite API tests for nutrition validation and persistence, expiry-day boundaries and alerts, allergen capture, and Recipe nutrition aggregation. The preexisting tests cover 401, 403, bad/expired/audience tokens, and some upstream error handling. **Measure code coverage** separately before claiming Issue #19's >85% threshold; these tests alone do not establish that coverage percentage. Add performance evidence for allergen filtering.

## Real integration tests required

1. Keycloak: Existing persisted realms **will not** update simply because `dev/user-identity/ctrl-eat-realm.json` changes. Apply the added client/scopes/audiences through the Admin UI or a separate fresh development realm; do **not** delete the shared Keycloak volume.
2. Start both new APIs (Ingredient `http://localhost:5161/`, Recipe `http://localhost:5162/`), client UIs (Ingredient `5185`, Recipe `5186`) and the existing Meal Plan, Grocery List and Storage Location apps. Their ports must agree with their launch profiles and local config.
3. Log in through each browser client, inspect granted scopes/audiences in a dev token **without publishing any token values**, create/edit/delete an Ingredient with nutrition and allergens, inspect expiring alerts, and create/update a Recipe using that Ingredient. Verify calculated macros and allergens.
4. Create a Daily Meal Plan referencing the new Recipe; create Grocery List and Storage Location entries referring to the new Ingredient. This tests the formerly mocked HTTP calls with real scoped tokens.
5. Repeat with missing and insufficient scopes (`401` and `403`) and check error handling. Keycloak audience/scope configuration is an integration dependency, not a reason to disable JWT checks.
6. Validate old database files in a disposable copy first, including the additive migrations. Have Sarah review old JSON data: **automatic migration of legacy JSON data is not included.**

## Human gate and submission requirements

- **Gate 1:** Link the original #19 work-item plan and record the developer's plan approval. Review target-layer dependencies and published DTO contracts.
- **Gate 2:** Developer records local warning-free build, full tests, code coverage, frontend verification, 401/403 traces (redact tokens), a diff/security inspection, and no committed secrets.
- **Gate 3:** Run the read-only review agent on the actual diff; obtain an independent teammate's review and approval on PR #22. *AI tools cannot stand in for human signoff.*
- Update the milestone work-tracking board statuses and 2–3 page reflection with actual evidence; do not claim a review that hasn't happened.
- Merge reviewed work to `feature/milestone-3-layered-service-development`, then open the required **Azure DevOps milestone PR** targeting the integration branch; include work item, milestone title, validation record and human signoff. Keep the milestone branch after merge; submit the **Azure DevOps PR URL** on Lea. A GitHub draft PR URL does not replace it.

## Remaining acceptance caveats

Issue #19 also requests a standalone alert **notification service**, >85% coverage for nutrition/allergen operations, and allergen-filter performance tests. This patch adds **on-demand expiry alerts** (not scheduled messages), plus EF-indexed allergen filtering; scheduled delivery, coverage threshold and performance results need separate implementation/measurement before #19 can be considered completely satisfied. No existing authorization settings were intentionally weakened.
