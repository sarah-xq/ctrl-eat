---
name: ctrl-eat-validate
description: Independently validate a Ctrl+Eat API implementation against its approved plan. Use after implementation to inspect the diff, run checks, and report verified results.
---

# Ctrl+Eat Validate

Review the implementation against the approved plan and acceptance criteria. When possible, use a fresh conversation or agent context so the review is not relying only on the implementer's assumptions. Begin read-only; do not silently repair findings during validation.

## Steps

1. Read the approved plan and inspect the full diff, including tests and configuration changes.
2. Check every acceptance criterion and look for unrelated edits, missing validation, incorrect HTTP behavior, direct JSON access outside repositories, and regressions in nearby behavior.
3. Run the planned build and test commands from the repository root when the .NET 10 SDK is available. The normal checks are `dotnet build CtrlEat.slnx` and `dotnet test CtrlEat.slnx`. If a command cannot run, report the exact reason. Never infer a pass from code inspection.
4. For API behavior not covered by automated tests, describe a practical manual request/check and its expected result. Do not report it as executed unless it was.
5. Compare actual results with the plan. If a finding requires code changes, return it as a specific handoff to `ctrl-eat-implement`, then validate again after the fix.

## Handoff

Return a Markdown validation report with:

- acceptance criteria marked **Pass**, **Fail**, or **Not verified**;
- commands run and their actual results;
- findings ordered by severity, with file paths when applicable;
- any manual checks still needed and known limitations.

Do not approve a merge, release, or deployment. The user remains responsible for reviewing the result.
