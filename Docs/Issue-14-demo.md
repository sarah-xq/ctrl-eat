# Run the daily meal planner

Use .NET 10 and the same local Keycloak realm as the User service. Start each component from the repository root in its own terminal.

1. Start Keycloak with `docker compose -f dev/user-identity/compose.yaml up`. A fresh import registers both clients. An existing realm skips import on restart. For an existing installation, add the meal-plan client, its audience mapper and its two scopes from the checked-in realm file, or start a separate fresh development instance with its own volume. Preserve the existing realm and its users.
2. Run `dotnet run --project CtrlEat.csproj --launch-profile http` for the Recipe and Ingredient HTTP lookups on port 5161.
3. Run `dotnet run --project src/DailyMealPlan.Api` for the meal-plan API on port 5104. It migrates its own SQLite database at startup. Check `http://localhost:5104/health`.
4. Run `dotnet run --project src/DailyMealPlan.Client` and open `http://localhost:5184/MealPlans`. Sign in or register a disposable local user. For a restricted local environment with no writable Windows key store, set `Demo__EphemeralSessions=true` in the client terminal. This development-only option expires existing browser sessions when the client restarts. Saved plans stay in the API database.

## Author and reviewer walkthrough

Create a plan with a date and no meals. Refresh and check the date and zero calories. Edit the date. Try an unknown recipe ID and check that the rejected edit preserves your input and the saved plan. Use a known recipe or ingredient from your local lookup service to check totals. Restart just the meal-plan API and reopen the saved plan. With a separate browser profile, sign in as a second user and check that the first account's plans are inaccessible. Delete the first plan as its owner. Record the checks you actually ran on the PR.

Run `dotnet build CtrlEat.slnx -warnaserror -m:1` and `dotnet test CtrlEat.slnx --no-build -m:1`. The integration tests use a temporary file database, a generated test signing key and HTTP lookup fixtures. Production uses discovery from the configured issuer. The demo uses the local Keycloak realm. Keep deployment keys and credentials outside source control.

## Boundary with the legacy service

The demo reads `GET /api/recipes/{id}` (name and totalCalories) and `GET /api/ingredients/{id}` (name and calories). Configure `LookupServices__RecipeBaseUrl` and `LookupServices__IngredientBaseUrl` when those owners split their services. Confirm their service-authentication policy before deploying. A missing reference returns 400 with the field, and a dependency failure returns 503. Each check occurs before a write.

The public numeric-user-ID meal-plan route is retired. The tracked JSON file contains an empty array. For an installation with existing records, back up and retain that file, map each numeric account to a verified issuer and subject, and compare counts and calorie totals before switching clients. Unmapped records must remain inaccessible.
