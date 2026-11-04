---
name: ctrl-eat-validate
description: Independently review a Ctrl+Eat Milestone 3 layered implementation for architecture, API security, frontend behavior and regression tests.
---

# Ctrl+Eat Validate
1. Read the approved work item, acceptance criteria and implementation handoff; inspect **all** changed files and tests.
2. Check dependency direction (Domain <- Data <- API; Client HTTP REST only), EF Core migrations, request/response DTOs, controller behavior, domain validation and cross-service references.
3. Check API JWT bearer configuration and named scopes/claim policies. Verify 401 missing/invalid/expired/wrong issuer/wrong audience, 403 valid token without policy, valid allowed scopes and explicitly public operations. Check no credentials or secrets are staged.
4. Run repo's actual solution build/test commands, e.g. baseline `dotnet build CtrlEat.slnx` and `dotnet test CtrlEat.slnx`; the new solution may have a different name. Record exact commands and outcomes, not assumed success. Verify frontend requests/errors with real checks if possible.
5. Provide Pass / Fail / Not verified per criterion; list findings by severity and exact affected path; distinguish automated test evidence from manual checks.
6. Return defects to layer owner. Revalidate after fixes.

**Human Gates 2 and 3:** A developer reviews the diff and test evidence; a separate teammate reviews and approves the PR. This agent cannot approve a PR in place of a human.
