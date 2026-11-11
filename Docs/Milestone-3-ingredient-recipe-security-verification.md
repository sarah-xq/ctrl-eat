# Ingredient and Recipe — Milestone 3 security verification

Status: **integration-test candidate for draft PR #22; not live Keycloak sign-off**.

## What is implemented

- Independent Ingredient and Recipe `Domain`, `Data`, `Api`, `Client` projects from PR #22.
- Ingredient API requires `ingredients.read` on GET and `ingredients.write` on mutations.
- Recipe API requires `recipes.read` on GET and `recipes.write` on mutations.
- Both APIs validate issuer, token signature, audience and expiry via ASP.NET Core JWT bearer authentication.
- Recipe sends the calling user's bearer token to Ingredient on HTTP lookup. The Recipe frontend development client requests `recipes.read`, `recipes.write`, and `ingredients.read` and is configured with both API audiences. This is a delegated user-token setup, not a privileged service token.
- Separate browser client registrations and four optional scopes are added to `dev/user-identity/ctrl-eat-realm.json`.

## Automated checks in this candidate

`tests/Ingredient.ApiTests/IngredientApiSecurityTests.cs` checks no-token 401, insufficient-scope 403, wrong-audience 401, expired-token 401, wrong-signature 401, public health response, authorized create/read, and invalid-input 400.

`tests/Recipe.ApiTests/RecipeApiSecurityTests.cs` checks no-token 401, insufficient-scope 403, wrong-audience 401, authorized HTTP Ingredient lookup via a simulated upstream, calories, missing-ingredient 400 with no persisted recipe, and missing Ingredient scope. These are **signed JWT middleware tests with static test signing keys**, but they are **not a live Keycloak test**. The Recipe upstream dependency is simulated to exercise the HTTP contract; it does not cryptographically validate the forwarded token at the simulated Ingredient endpoint.

To run locally:

```powershell
dotnet build CtrlEat.slnx -warnaserror -m:1
dotnet test CtrlEat.slnx --no-build -m:1
```

## Required human and live-system checks

1. **Existing Keycloak realm:** Keycloak `start-dev --import-realm` only imports a missing realm; it does not update an already persisted realm. If the realm already exists, add the two clients, four scopes and dual audience mapper through Keycloak administration or import into a separate new dev instance. Do not delete volumes or accounts merely to reload configuration.
2. Run Ingredient API on `http://localhost:5161`, Recipe API on `http://localhost:5162`; browser clients on `http://localhost:5185` and `http://localhost:5186`. Ensure redirect URIs and OAuth client scope mappings match.
3. Confirm the Recipe frontend token has **both** `ctrleat-recipes` and `ctrleat-ingredients` audiences, plus `recipes.write` and `ingredients.read` scopes. Run a live Recipe create operation referencing a saved Ingredient.
4. Test `401` with no token and wrong audience, `403` with insufficient scopes, and success with correct scopes on both APIs. Save evidence of responses and independent browser flows.
5. Check that Daily Meal Plan, Storage Location and Grocery List services are configured to use the new Ingredient/Recipe URLs and have an **authorized downstream identity**. Their existing HTTP clients may need bearer-token delegation or a service-to-service token; do not disable Ingredient/Recipe authentication to make these calls pass.
6. Have Sarah (or an authorized teammate) review the replacement architecture, DB migration strategy, API behavior, and UI. Existing JSON data is **not** migrated automatically. Issue #19 nutritional fields, allergen tagging and expiry alerts remain unimplemented in this candidate.

**Do not mark the service migration fully completed or merge PR #22 until these validations and human approval are recorded.**
