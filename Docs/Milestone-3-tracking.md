# Milestone 3 — Team Work Tracking

Evidence snapshot: November 10, 2026. This is a repository export supporting, but not replacing, the required published GitHub Projects/Azure Boards board. Checkboxes reflect evidence, not assumptions.

| Owner | Service | Work item / pull request | Status and next gate |
| --- | --- | --- | --- |
| Félix (`Louis-FelixRDC`) | User | [Issue #12](https://github.com/sarah-xq/ctrl-eat/issues/12), [merged PR #13](https://github.com/sarah-xq/ctrl-eat/pull/13) | Layered API, database and React client merged. Recheck on final integrated branch. |
| Félix | Daily Meal Plan | [Issue #14](https://github.com/sarah-xq/ctrl-eat/issues/14), [merged PR #16](https://github.com/sarah-xq/ctrl-eat/pull/16) | Merged November 11, 2026 (052c03d). Integrated .NET build passed and 169/169 tests succeeded. Independent functional and diff review remains to be recorded. |
| Zachary (`ZacharyAbergel`) | Storage Location | [merged PR #18](https://github.com/sarah-xq/ctrl-eat/pull/18) | Domain/Data/Api and frontend files merged; work item hierarchy, security tests and signoff need confirmation. |
| Zachary | Grocery List | [merged PR #18](https://github.com/sarah-xq/ctrl-eat/pull/18) | Domain/Data/Api and frontend files merged; work item hierarchy, security tests and signoff need confirmation. |
| Sarah (`sarah-xq`) | Ingredient | [legacy PR #1](https://github.com/sarah-xq/ctrl-eat/pull/1), [open Issue #19](https://github.com/sarah-xq/ctrl-eat/issues/19) | Milestone 3 independent layers, frontend, security and testing not verified. |
| Sarah | Recipe | [legacy PR #1](https://github.com/sarah-xq/ctrl-eat/pull/1) | Milestone 3 independent layers, frontend, security and testing not verified. |

## Félix — Issue #12 (User)
Acceptance criteria: a signed-in user can view/save a positive calorie goal, retain it after restart, and cannot access another user's data; invalid input yields 400 without changing the saved goal; invalid/missing tokens yield 401 and insufficient scopes 403.

- [x] Planner research and human approval recorded in `Docs/Issue-12-plan.md`
- [x] Domain validation, EF Core persistence and migration
- [x] API DTOs, bearer token validation, separate read/write scope policies
- [x] React client over HTTP with OIDC PKCE
- [x] Tests, browser verification and peer review documented in PR #13
- [ ] Verify again after milestone integration

## Félix — Issue #14 (Daily Meal Plan)
Acceptance criteria: authenticated dated plan create/list/edit/delete, saved changes survive restart, invalid recipe/ingredient references return 400 without partial writes, accurate calorie totals, user ownership, 401/403 and public health endpoint.

- [x] R-PIV plan reviewed and approved, Razor Pages selected
- [x] Domain model/calorie unit tests
- [x] EF Core SQLite repositories and migration
- [x] API security, owner filtering and HTTP recipe/ingredient lookups
- [x] Razor client via REST and OIDC PKCE
- [x] Historical Validator report for `af17fbd`: 0 build errors/warnings, 21 tests passed; peer approval by Zachary
- [x] Resolve PR #16 conflicts without discarding newer team changes
- [x] Integrated .NET build and 169/169 automated tests passed; frontend/browser revalidation remains pending
- [ ] Félix performs and records *his own* code diff and live functional/security review
- [x] PR #16 merged on November 11, 2026 (052c03d)

## Zachary — Storage Location and Grocery List

- [x] Layered code, migrations, frontend views and test projects included in merged PR #18
- [ ] Confirm assigned parent work items/epics, acceptance criteria and 3–6 granular tasks per capability on the published board
- [ ] Confirm Ingredient HTTP contract and deployment identity-provider registration
- [ ] Record actual automated API tests (401, invalid token, wrong scope 403, allowed operation, public health), browser checks and warning-free integrated build
- [ ] Record review-agent report, code-diff review and teammate signoff

## Sarah — Ingredient and Recipe

- [ ] Link parent work items, assignments, acceptance criteria and 3–6 subtasks per capability
- [ ] Verify two independent runnable Domain/Data/Api services and corresponding HTTP frontend views
- [ ] Verify EF Core migrations, DTO contracts, endpoint policies, OIDC authentication, tests and PR links
- [ ] Record genuine AI agent plans, human corrections, validations and peer approval

## Shared Milestone 3 submission checklist

- [ ] At least two independent layered services per team member, frontend/client integration, and one-way project references
- [ ] Published GitHub Projects or Azure DevOps board with epics, stories, granular tasks, assignments, acceptance criteria and states
- [ ] Required custom agents and skill definitions verified (the branch contains `.github/agents` and `.github/skills`)
- [ ] Every API's auth policy/scope mapping and tests confirmed (see `Milestone-3-authentication-and-validation.md`)
- [ ] The 2–3 page architecture and reflection report addresses all four rubric topics
- [x] PR #16 conflicts resolved and integrated .NET build/test results recorded
- [ ] R-PIV plans, human verification, peer approvals and security signoffs recorded for each applicable PR
- [ ] Milestone-level **Azure DevOps** PR created against normal integration branch, branch retained, PR link submitted on Lea

## Integration verification after resolving PR #16 merge conflicts

The author reported the following *locally executed* checks on branch `issue-14-daily-meal-plans`, after integrating the latest `feature/milestone-3-layered-service-development` changes. The conflict in `CtrlEat.slnx` was resolved by preserving projects from both branches; 27 projects were listed in the combined solution.

- `git diff --cached --check` — no whitespace errors reported.
- `git ls-files -u` — no unmerged paths reported.
- `dotnet build CtrlEat.slnx -warnaserror -m:1` — succeeded (the 27 projects compiled with warnings treated as errors).
- `dotnet test CtrlEat.slnx --no-build -m:1` — **169 total, 169 passed, 0 failed, 0 skipped**, as reported in the terminal output.
- Merge commit **`24d9ae5`** was pushed to `origin/issue-14-daily-meal-plans`; `git status -sb` showed a clean branch tracking the remote.

These are meaningful **integrated .NET build and automated test results**, but do not replace browser-based checks for every frontend, the author's independent diff/security review, teammate approval of the integrated revision, or verification of independent Ingredient and Recipe services. This information was supplied by the person executing the terminal commands; it is not represented as a separate CI run or independently repeated by the documentation preparer.

## Published GitHub Project

[CtrlEat - Milestone 3](https://github.com/users/Louis-FelixRDC/projects/1)

The public board contains seven epics, 31 draft task cards, and existing repository issues and PRs. Tasks record ownership, acceptance criteria, and status; remaining verification is tracked separately.
