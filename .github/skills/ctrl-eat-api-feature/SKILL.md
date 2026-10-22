---
name: ctrl-eat-api-feature
description: Run the complete R-PIV workflow for a feature or bug in the Ctrl+Eat ASP.NET Core API. Use when a request needs research, a reviewed plan, implementation, and validation.
---

# Ctrl+Eat API Feature Workflow

Use this skill as the entry point for a feature or bug that changes the Ctrl+Eat API. **R-PIV means Research, Plan, Implement, Validate.** It coordinates the four focused skills in this folder: `ctrl-eat-research`, `ctrl-eat-plan`, `ctrl-eat-implement`, and `ctrl-eat-validate`.

## Workflow and handoffs

1. **Research:** Inspect the issue and relevant code without editing files. Return the current behavior, constraints, likely files, and unanswered questions. Use `ctrl-eat-research`.
2. **Plan:** Turn the confirmed requirements and research into a small, ordered plan with acceptance criteria and a validation strategy. Show it to the user and wait for approval before implementation. Use `ctrl-eat-plan`.
3. **Implement:** Work only from the approved plan, preferably in a fresh context using the plan as the handoff. If implementation reveals a material assumption or scope change, stop and bring it back for a decision. Use `ctrl-eat-implement`.
4. **Validate:** Compare the completed diff against the plan and run the available build and tests. Prefer a fresh conversation or agent context for this review. Use `ctrl-eat-validate`.
5. **Evolve:** If the work exposed a repeatable gap in the project’s AI guidance, use `ctrl-eat-ai-layer-evolution` to decide whether a small reusable rule or skill improvement is justified.

Each stage should produce a concise Markdown handoff that can be pasted into or saved for the next stage. Keep the human involved in confirming requirements, approving the plan, and reviewing validation results. Do not automate merges, releases, or deployments.

## Ctrl+Eat project conventions

- Keep HTTP routing and status codes in `Controllers/`.
- Keep validation, business rules, and orchestration in `Services/`.
- Access JSON data through `Repositories/`; do not read or write data files directly from controllers or services.
- Keep API contracts in `DTOs/` and domain types in `Models/`.
- Follow the patterns of nearby files. The application currently registers JSON-backed repositories and services in `Program.cs`.
- Do not add a database, framework, package, or architecture layer unless the approved plan requires it.
- Keep changes tied to the current issue. Do not commit, push, open a pull request, or deploy unless the user explicitly requests that action.

## Completion report

Summarize the behavior changed, files or layers touched, validation commands and actual results, and any remaining limitation or deferred finding. Never report a command as passing unless it was run successfully.
