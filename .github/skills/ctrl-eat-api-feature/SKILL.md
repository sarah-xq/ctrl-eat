---
name: ctrl-eat-api-feature
description: Coordinate research, planning, layered implementation and independent validation for a Ctrl+Eat Milestone 3 feature.
---

# Ctrl+Eat Milestone 3 R-PIV Workflow
1. **Research:** Use `ctrl-eat-researcher` and `ctrl-eat-research` to inspect the actual code and required feature.
2. **Plan:** Use `ctrl-eat-planner` / `ctrl-eat-plan` to produce acceptance criteria, contract decisions, and file-by-file steps. **Stop for human approval.**
3. **Implement:** Use `domain-dev`, `data-dev`, `api-dev`, `frontend-dev` as needed, normally in that order; `ctrl-eat-implementer` can coordinate a narrowly approved cross-layer step. Each agent has limited layer ownership.
4. **Validate:** Use `ctrl-eat-validator` / `ctrl-eat-validate` independently. Build/test, review security, and record actual results. Return findings to the responsible developer.
5. **Peer review:** Have another human inspect the PR, review the human-gate checklist and approve. Do not automate approval, merge or deployment.
6. **Evolve:** Optionally use `ctrl-eat-ai-layer-evolution` for recurring agent mistakes.

The optional `ctrl-eat-dispatcher` only recommends **which agent to use next**. It does not invoke agents automatically.

## Milestone 3 conventions
- Each service is independently runnable and divided into `.Domain`, `.Data`, `.Api` under `src/`, with corresponding unit and integration tests.
- Domain is pure business logic; Data uses EF Core; API has thin controllers, DTOs, application services, bearer authentication and scope/claim policies; Client uses REST HTTP only.
- Existing JSON-backed services are migration sources; do not preserve old persistence constraints when migrating to EF Core.
- Track work items with clear owners, 3-6 tasks and testable acceptance criteria. Link commits and PRs to work items.
- Never claim validation or human approval without evidence. No unexpected commits, PRs, merges or deployments.
