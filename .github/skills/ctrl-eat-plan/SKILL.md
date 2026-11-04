---
name: ctrl-eat-plan
description: Prepare a human-reviewed implementation plan and acceptance criteria for a Ctrl+Eat Milestone 3 work item.
---

# Ctrl+Eat Plan
1. Confirm the assigned parent work item, owner and acceptance criteria. Break it into **3-6 focused tasks** when appropriate and identify corresponding layer owners.
2. Freeze the relevant endpoints, HTTP methods, auth scopes, and request/response DTO shapes **before** parallel frontend/backend changes.
3. Propose ordered file changes: `.Domain` (pure rules and entities), `.Data` (EF Core, DbContext, configurations, migrations, repositories), `.Api` (DTOs, thin controllers, application services, policies), `.Client` (HTTP REST calls, screens, errors) and `tests/`.
4. Keep project reference direction one-way: Domain has no external web/data dependencies; Data references Domain; API references Data and Domain; Client does not reference internal server assemblies. If services reference each other, prefer API calls and published contracts rather than cross-database access.
5. Specify error cases and tests, including 401 (no/invalid token), 403 (insufficient scope), allowed scopes, and public endpoints if relevant. State exact build/test commands found in the repo; existing baseline is `dotnet build CtrlEat.slnx` and `dotnet test CtrlEat.slnx`, but new solution names may differ.
6. List risks, potential schema/data migration decisions, agent assignments, and human review checkpoints.

**Gate 1:** Present a numbered Markdown plan. No implementation until human approval. No code edits during planning.
