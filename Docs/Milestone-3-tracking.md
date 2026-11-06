# Milestone 3 work tracking · Félix

Snapshot of the assigned User and DailyMealPlan stories on November 5, 2026. This is a repository work-item export. Team members can use their own issues to complete the shared board.

| Epic | Work item and owner | Acceptance criteria | Evidence and state |
| --- | --- | --- | --- |
| Authenticated User service | [#12](https://github.com/sarah-xq/ctrl-eat/issues/12), Louis-FelixRDC | Save a positive goal, retain it after restart, reject invalid input, restrict access to the signed-in account | [PR #13 approved and merged](https://github.com/sarah-xq/ctrl-eat/pull/13). Follow-up in #14 covers failed profile reads and legacy build warnings. |
| Authenticated meal planning | [#14](https://github.com/sarah-xq/ctrl-eat/issues/14), Louis-FelixRDC | Manage dated plans, validate meal/snack references, persist totals and enforce ownership | [Plan approved](https://github.com/sarah-xq/ctrl-eat/issues/14#issuecomment-6664191431). Implementation and automated tests complete. Human verification and independent signoff pending. |

| #14 task | Evidence | State |
| --- | --- | --- |
| Agree on plans, endpoints and client | Planner proposal and approval in the issue. ASP.NET Core selected. | Approved. Deployment lookup contract confirmation belongs to the Recipe and Ingredient owners. |
| Extract meal and snack rules | DailyMealPlan.Domain and its unit tests | Implemented |
| Store plans and snacks | EF Core SQLite migration, repository, restart integration test | Implemented |
| Expose scoped operations | DailyMealPlan.Api and HTTP lookup clients | Implemented |
| Connect a browser client | DailyMealPlan.Client Razor Pages and OIDC code flow with PKCE | Implemented |
| Validate and review | API security tests, owner isolation, outage and invalid-reference scenarios | Automated checks passed. Author and teammate signoff pending. |
