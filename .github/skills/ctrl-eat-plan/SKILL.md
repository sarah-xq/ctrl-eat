---
name: ctrl-eat-plan
description: Create a reviewable implementation and validation plan for a Ctrl+Eat API change. Use after research or when the request is already clear enough to plan.
---

# Ctrl+Eat Plan

Create a small, implementation-ready plan from the confirmed request and research findings. If research is missing, inspect enough of the relevant code to ground the plan; do not invent architecture.

## Plan contents

1. Restate the expected behavior and acceptance criteria in testable terms.
2. List the ordered changes, naming the files and responsibilities. Keep each task focused.
3. Identify validation and error cases, including relevant HTTP responses and data validation.
4. Specify tests to add or update in `CtrlEat.Tests/`, and the build/test commands that can verify the work (normally `dotnet build CtrlEat.slnx` and `dotnet test CtrlEat.slnx` from the repository root). Include a manual API check through `CtrlEat.http` only when the changed behavior needs a request-level check.
5. Note assumptions, risks, and any decision still required from the user.

Follow the project boundaries: controllers handle HTTP, services handle business logic, repositories handle JSON persistence, DTOs define API contracts, and models define domain data. Do not plan a new framework, database, or broad refactor for a narrow issue.

## Review gate

Present the plan as a numbered Markdown handoff. Do not edit implementation files while planning. Wait for the user to approve or revise the plan before `ctrl-eat-implement` begins. A small change may have a short plan, but it still needs explicit scope and a way to verify it.
