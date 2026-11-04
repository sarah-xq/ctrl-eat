---
name: ctrl-eat-research
description: Read-only investigation for Ctrl+Eat Milestone 3 layered services, EF Core migrations, REST APIs and frontend features.
---

# Ctrl+Eat Research
1. Read the specific work item, user story and acceptance criteria.
2. Inspect affected existing files: controllers, service classes, JSON repositories, models, DTOs, tests, configuration, and related calls. Check which new layered projects already exist; do not assume the migration is finished.
3. Trace **current** behavior and compare to the **required** Domain -> Data -> API separation and HTTP-only Client integration. Note where business rules live, cross-service references, and authentication requirements.
4. Identify shared API contracts and any breaking changes before parallel implementation.
5. Return **Goal, Verified current behavior (paths), Intended target, Dependencies, Likely changes, Tests, Open decisions**. Separate facts from assumptions.

Read-only; do not edit code or invent implementation results.
