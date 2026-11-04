---
name: ctrl-eat-research
description: Research a Ctrl+Eat API feature, bug, or issue before planning or changing code. Use for a read-only investigation of current behavior, code paths, project conventions, and open questions.
---

# Ctrl+Eat Research

Research the requested change without editing project files. The goal is to give the user and the planning stage an accurate picture of the existing code, not to guess at an implementation.

## Steps

1. Read the complete issue, acceptance criteria, or user description. Separate explicit requirements from assumptions.
2. Search for related endpoints, DTOs, services, repositories, models, tests, configuration, and callers. Inspect the relevant files rather than relying on names or summaries alone.
3. Trace the current request and data flow. For this project, check the applicable controller → service → repository path and any related JSON data or cross-service call.
4. Check nearby code for established naming, validation, status-code, and error-handling patterns. Note gaps in test coverage.
5. Identify the smallest plausible set of files or layers affected, risks, and decisions that need the user's input.

## Handoff

Return a concise Markdown research note with:

- **Goal and acceptance criteria:** what the request explicitly asks for.
- **Current behavior:** relevant endpoint and data flow, with file paths.
- **Constraints and conventions:** patterns or dependencies that the plan should preserve.
- **Likely change surface:** files/layers that may need work and why.
- **Validation opportunities:** existing tests and practical build or API checks.
- **Open questions and assumptions:** ask only about choices that materially affect behavior or architecture.

Do not edit code, generate a solution, or silently turn an assumption into a requirement. This handoff feeds `ctrl-eat-plan`.
