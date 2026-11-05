# Try the issue 12 calorie-goal demo

From the repository root, start the three components in separate terminals. The demo uses .NET 10, Node 22 and Docker Compose with Linux containers.

1. Start Keycloak with `docker compose -f dev/user-identity/compose.yaml up`. The supplied development realm allows local demo registration. Its issuer is http://localhost:8082/realms/ctrl-eat.
2. Run `dotnet run --project src/User.Api`. This starts the API at http://localhost:5103 and applies the EF Core migration to a local SQLite database.
3. In `src/User.Client`, run `npm ci` and `npm run dev`. Open http://localhost:5173. Select **Sign in**, register a disposable local account, and set a goal. Reload to see the saved value. Try zero to see the validation error.

## Contract and security

The React client signs the resource owner in with Keycloak using authorization code and S256 PKCE. User.Api is the resource server. Its JWT bearer handler checks the signature, issuer, audience (ctrleat-users) and expiry. Profiles are selected by validated issuer and subject. The API returns only calorieGoal, initially null. Environment variables Authentication__Authority, Authentication__Audience, ConnectionStrings__User and Cors__ClientOrigin configure deployment settings. The local example uses loopback HTTP for development. Use HTTPS and the team's registered client settings for deployment.

| Request | Policy | Result |
| --- | --- | --- |
| GET /health | Public | 200 |
| GET /api/users/me | users.read | Current goal |
| PUT /api/users/me/calorie-goal | users.write | Saves a positive integer |

The request body for an update is `{"calorieGoal":2100}`. Missing or invalid credentials return 401, insufficient scope returns 403, and invalid input returns 400. The old unauthenticated User controller was retired. The repository's legacy user data file is empty. Any separate deployment with existing users needs verified identity linking before switching to this database.

## Evidence and review

Visual Studio Copilot's Planner produced the plan in Issue-12-plan.md. Félix approved the separate projects and goal-only response. The Domain, Data, API and Frontend agents implemented their layers. During validation, the root project was corrected to exclude nested projects and the demo issuer was configured to emit the subject claim explicitly.

Validated locally with the Keycloak distribution and the imported realm, the User API, and Vite. Browser verification covered S256 sign-in, first-use empty state, save, page reload and invalid input. The API integration suite also checked database persistence across API hosts, ownership isolation, token rejection and separate read/write scopes. Run `dotnet test CtrlEat.slnx` and `npm run build` inside the client for repeatable automated checks. The 11 existing tests and 5 new tests passed. The full solution still has 73 pre-existing compiler warnings. Human diff review, peer signoff and a milestone-level Azure DevOps PR remain open.
