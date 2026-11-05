---
name: ctrl-eat-implement
description: Implement a human-approved Milestone 3 Ctrl+Eat task using Domain, Data, API and Client boundaries.
---

# Ctrl+Eat Implement
1. Read the approved plan and inspect git status. Preserve unrelated changes and work only on the assigned task.
2. Implement only the approved layers. Move existing behavior where possible rather than rewriting from scratch. JSON repositories are legacy source code: when the plan says EF Core migration, implement EF Core within `.Data`; do not keep JSON persistence merely because older instructions did.
3. Keep Domain free of EF Core and ASP.NET Core, Data independent of API and Client, controllers thin, public contracts as DTOs. The Client uses HTTP REST only.
4. Use ASP.NET Core JWT bearer authentication with validated issuer, audience, signature, and expiry; use named scope/claim authorization policies. Do not embed keys/secrets, invent login handlers, or store provider passwords.
5. Add/adjust focused tests, do not silently change shared API contracts. If a material contract or architecture decision is needed, stop and get human approval of revised scope.
6. Hand off changed files, actual commands run, results, acceptance criteria left unverified and review notes.

Do not commit, push, open or merge PRs unless explicitly requested. Build/tests only count when actually executed.
