---
name: ctrl-eat-implement
description: Implement an approved Ctrl+Eat API plan in small, scoped changes. Use only after the user has approved the plan or has explicitly asked for a direct implementation.
---

# Ctrl+Eat Implement

Implement the approved plan, preserving the project’s existing patterns. If no approved plan or direct implementation request is available, return a plan request instead of making code changes.

## Steps

1. Re-read the approved plan and inspect the current working tree. Preserve unrelated user changes. When practical, begin in a fresh context with the approved plan as the handoff.
2. Make the smallest changes that satisfy the approved acceptance criteria. Keep each edit focused and report meaningful progress.
3. Keep controllers focused on HTTP behavior, business rules in services, and JSON access in repositories. Use DTOs at the API boundary and follow nearby naming and error-handling patterns.
4. Add or update focused tests for changed behavior when practical. Do not add dependencies, a database, or unrelated cleanup unless the plan calls for it.
5. If the plan proves incomplete or an important assumption is false, stop before expanding scope. Explain the finding and ask for a decision or an updated plan.
6. Provide a concise handoff listing the changes made and what still needs validation by `ctrl-eat-validate`.

Do not claim the change is validated until the relevant commands and checks have actually run. Do not commit, push, open a pull request, or deploy unless explicitly requested.
