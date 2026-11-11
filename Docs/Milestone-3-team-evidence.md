# Milestone 3 — Team Architecture, AI Development and Human Gate Evidence

Companion to the required 2–3 page reflection. Evidence snapshot: November 10, 2026. Incomplete portions are flagged rather than invented.

## Layered architecture and frontend integration

The original CtrlEat application is an ASP.NET Core API organized around controllers, services, models and JSON repositories. In Milestone 3 the team separates service capabilities into **Domain** (independent business models and pure rules), **Data** (EF Core repositories, contexts and SQLite migrations), **API** (public request/response DTOs, endpoint contracts, application orchestration and bearer-token policies), and **Client** (React or ASP.NET Core web views consuming the APIs over HTTP only).

`User.Domain/Data/Api` and React/Vite `User.Client` implement authenticated calorie-goal management. `DailyMealPlan.Domain/Data/Api/Client` implements authenticated date-based meal planning with Razor Pages; the service queries Recipe and Ingredient resources over HTTP and stores plans in SQLite after validating references. Daily Meal Plan remains on open PR #16. Storage Location and Grocery List each gained layered projects and frontend/test files in merged PR #18. Separate Milestone 3 Ingredient and Recipe layered services have not yet been verified.

## Work allocation

Félix owns **User** ([Issue #12](https://github.com/sarah-xq/ctrl-eat/issues/12), merged [PR #13](https://github.com/sarah-xq/ctrl-eat/pull/13)) and **Daily Meal Plan** ([Issue #14](https://github.com/sarah-xq/ctrl-eat/issues/14), open [PR #16](https://github.com/sarah-xq/ctrl-eat/pull/16)). Zachary owns **Storage Location and Grocery List** (merged [PR #18](https://github.com/sarah-xq/ctrl-eat/pull/18)). Sarah owns **Ingredient and Recipe**, which have historical implementations in [PR #1](https://github.com/sarah-xq/ctrl-eat/pull/1); the separate Milestone 3 architecture, client integration and policy/test evidence remain unverified. See `Milestone-3-tracking.md` for outstanding tasks and team-wide board evidence requirements.

## AI agents: benefits and manual corrections

The team uses **Research → Plan → Implement → Validate (R-PIV)**. For #12 the repository Copilot Planner proposed the vertical slice and Félix approved separate projects and a goal-only DTO. Domain/Data/API/Frontend agents implemented the layers. Human-initiated follow-ups corrected nested-project compilation and subject claim configuration. For #14 the Planner's proposal was reviewed, the client stack was intentionally changed from the issue's React suggestion to Razor Pages, and specialized agents implemented the service before independent validation. The benefits were smaller, layer-focused work units and a repeatable check sequence. Agent output still required human decisions about architecture, external contracts, identity configuration, tests and failed requests. Agent use/manual corrections by other team members need their own genuine evidence.

## Human gate effectiveness and evidence

**Gate 1 — planning:** Issue #12 plan and approval are in `Issue-12-plan.md`; #14's approved plan is linked in PR #16. **Gate 2 — code and tests:** for PR #16 head `af17fbd`, the independent [Validator report](https://github.com/sarah-xq/ctrl-eat/pull/16#issuecomment-6692620471) records a warning-free build, 21/21 .NET tests, a clean diff check, and no actionable findings. Browser-assisted checks covered login, create/reload, rejected references with input retention, date edit and deletion; API tests covered separate users and dependency failures. **Gate 3 — peer review:** Zachary's PR #16 review is APPROVED. However, Félix's own local functional/diff review has not been recorded. PR #16 now conflicts with milestone changes, so the integrated build/tests and renewed human review are needed before merging. Recording human gates catches risks that automated tests alone do not prove, but existing results must not be generalized to untested integrations.

## Required follow-up

- Confirm each member has at least two independent runnable Domain/Data/Api services with frontend integration; especially verify Ingredient/Recipe.
- Publish the full work tracking board with epics, stories, 3–6 tasks each, assignments, acceptance criteria and state.
- Confirm all services' scopes, token validation, security tests and endpoint mapping in `Milestone-3-authentication-and-validation.md`.
- Resolve PR #16 conflicts without overwriting PR #18 changes; run integrated build, .NET tests, frontend builds and browser/API security tests.
- Complete Félix's personal verification and team peer signoff without falsely checking them off.
- Verify that the existing required **2–3 page reflection PDF** includes all four topics. This supplementary document does not replace the PDF's length requirement.
- Open a **milestone-level Azure DevOps PR**, retain its branch after merge, and submit that PR URL on Lea, as specified in the rubric.

## Integration verification after resolving PR #16 merge conflicts

The author reported the following *locally executed* checks on branch `issue-14-daily-meal-plans`, after integrating the latest `feature/milestone-3-layered-service-development` changes. The conflict in `CtrlEat.slnx` was resolved by preserving projects from both branches; 27 projects were listed in the combined solution.

- `git diff --cached --check` — no whitespace errors reported.
- `git ls-files -u` — no unmerged paths reported.
- `dotnet build CtrlEat.slnx -warnaserror -m:1` — succeeded (the 27 projects compiled with warnings treated as errors).
- `dotnet test CtrlEat.slnx --no-build -m:1` — **169 total, 169 passed, 0 failed, 0 skipped**, as reported in the terminal output.
- Merge commit **`24d9ae5`** was pushed to `origin/issue-14-daily-meal-plans`; `git status -sb` showed a clean branch tracking the remote.

These are meaningful **integrated .NET build and automated test results**, but do not replace browser-based checks for every frontend, the author's independent diff/security review, teammate approval of the integrated revision, or verification of independent Ingredient and Recipe services. This information was supplied by the person executing the terminal commands; it is not represented as a separate CI run or independently repeated by the documentation preparer.
