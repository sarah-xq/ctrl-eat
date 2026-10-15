---
name: ctrl-eat-api-feature
description: Research, plan, implement, and validate small API features in the Ctrl+Eat ASP.NET Core Web API. Use this skill when adding or modifying controllers, services, repositories, DTOs, models, or tests.
---

# Ctrl+Eat API Feature Skill

Use this workflow when implementing a small feature or GitHub issue in
the Ctrl+Eat project.

## 1. Research

Before changing code:

1. Read the complete GitHub issue.
2. Search for related issues and existing code.
3. Inspect the relevant controller, service, repository, DTO, and model.
4. Explain how the current feature works.
5. Identify the smallest set of files that may need to change.
6. Do not modify code during the research step.

Stop and present the research findings for approval.

## 2. Plan

Create a numbered implementation plan that includes:

- files to modify
- files to create
- responsibility of each change
- validation rules
- expected HTTP responses
- test cases
- build and test commands

Keep the change limited to the current GitHub issue.

Do not implement the plan until it has been reviewed and approved.

## 3. Implement

After the plan is approved:

1. Create or use a feature branch.
2. Make one small change at a time.
3. Keep HTTP handling in controllers.
4. Keep business rules in services.
5. Access JSON data only through repositories.
6. Use DTOs for API request and response contracts.
7. Follow the naming and error-handling style of nearby files.
8. Do not introduce a database or new framework.
9. Do not modify unrelated services.
10. Summarize each meaningful change before continuing.

## 4. Validate

After implementation:

1. Build the solution.
2. Run all automated tests.
3. Add tests for successful and unsuccessful behavior.
4. Check every acceptance criterion from the GitHub issue.
5. Report the commands used and their actual results.
6. Do not claim that a build or test passed unless it was run successfully.

## 5. Review

Before opening a pull request:

1. Review the complete diff.
2. Look for unrelated changes.
3. Confirm that business logic is not placed in controllers.
4. Confirm that JSON files are accessed only through repositories.
5. Check error handling and validation.
6. Record each finding as fixed or deferred.
7. If a finding is deferred, explain why.

## 6. Pull request

Prepare a pull request that includes:

- a link to the GitHub issue
- a summary of the implementation
- files or layers changed
- test evidence
- known limitations
- any deferred review findings

Show the proposed pull request title and description before creating it.
Never merge the pull request automatically.