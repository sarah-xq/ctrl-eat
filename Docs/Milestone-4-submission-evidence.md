# Milestone 4 submission evidence

## Scope

Milestone 4 protects the service-oriented application and documents security
design. This record identifies implementation branches, pull requests,
verification evidence, peer-review gates, and remaining submission steps.

## Branch and PR map

| Issue | Branch | Draft PR | Evidence |
| --- | --- | --- | --- |
| #24 | `feature/milestone-4-security` | Add separately for milestone integration | Authentication/authorization implementation commit |
| #25 | `issue-25-profile-picture-contract` | #33 | Domain/Data contract and migration |
| #26 | `issue-26-profile-picture-api` | #34 | Private API/storage implementation |
| #27 | `issue-27-security-tests` | #35 | Unit/integration/contract test matrix |
| #28 | `issue-28-cross-device-picture` | #36 | Cross-device workflow and manual runbook |
| #29 | `issue-29-threat-model` | #32 | Threat model and residual risks |
| #30 | `issue-30-milestone-evidence` | This PR | Submission and integration evidence |

The feature branch is based on `main` and remains available after milestone
integration. Milestone implementation work must not be pushed directly to
`main`. Sub-branches are stacked or rebased only through peer-reviewed PRs.

## Repository integration branch

The usual repository integration branch is `main`. Use the Milestone 4 feature
branch as the implementation integration branch:

```bash
git switch main
git pull --ff-only
git switch feature/milestone-4-security
git pull --ff-only origin feature/milestone-4-security
```

Do not delete `feature/milestone-4-security` after a merge.

## Verification commands

Run the repository solution and client before final submission:

```bash
dotnet build CtrlEat.slnx
dotnet test CtrlEat.slnx
cd src/User.Client
npm ci
npm run build
```

Use the smallest targeted commands while resolving failures:

```bash
dotnet build src/User.Api/User.Api.csproj --no-restore
dotnet build tests/User.UnitTests/User.UnitTests.csproj --no-restore
dotnet build tests/User.IntegrationTests/User.IntegrationTests.csproj --no-restore
dotnet test tests/User.UnitTests/User.UnitTests.csproj --no-build --no-restore
dotnet test tests/User.IntegrationTests/User.IntegrationTests.csproj --no-build --no-restore
```

## Recorded environment result

At implementation time, project builds completed under the available SDK.
Testhost execution was blocked because the environment has only .NET 11 while
the solution test projects target .NET 10. Install the .NET 10 runtime, rerun
both test projects and the solution test command, and replace this limitation
with pass/fail evidence before final submission.

The client build passed after `npm ci` for issue #24. No secret, provider
password, bearer token, signing key, or storage credential is recorded in this
evidence.

## Security evidence

- Issue #24: JWT issuer/audience/signature/lifetime validation, named scopes,
  identity claims, environment-specific configuration, and sign-out behavior.
- Issue #25: 5 MiB JPEG/PNG/WebP domain rules, signature checks, EF Core
  profile-picture reference, and storage contract.
- Issue #26: authorized upload/read routes, private server-side storage, cleanup
  behavior, generic failure responses.
- Issue #27: authentication/authorization, upload, cross-user, persistence,
  storage-failure, and secret-exposure assertions.
- Issue #28: separate Device A/Device B API workflow and browser/manual runbook.
- Issue #29: threat model, mitigations, residual risks, and test mapping.

## Peer-review gate

Each implementation PR requires a reviewer who is not the implementation owner.

Before merge, reviewers must verify:

- scope and acceptance criteria;
- API/identity contract and status codes;
- no source/client secrets;
- private profile-picture access;
- deterministic tests and persisted observable state;
- no unrelated changes;
- review feedback disposition.

## Azure DevOps submission

This repository session does not expose a configured Azure DevOps remote or PR
endpoint. A team member must create the Azure DevOps PR from the usual
integration branch with title:

```text
Milestone 4: Security
```

The Azure DevOps PR description must include the concise change summary,
security validation results, review notes, this GitHub evidence, and the final
Azure DevOps PR URL. Submit that URL on Lea and keep the feature branch after
merge.

## Milestone PR evidence checklist

- [ ] .NET 10 runtime installed.
- [ ] Solution build passes.
- [ ] Unit tests execute and pass.
- [ ] Integration tests execute and pass.
- [ ] Contract tests execute and pass.
- [ ] Cross-device verification evidence is recorded.
- [ ] Manual sign-out/independent-device verification is recorded.
- [ ] Every implementation PR has peer review.
- [ ] Threat model mitigations map to tests or manual checks.
- [ ] Azure DevOps PR is created and linked in issue #30.
- [ ] `feature/milestone-4-security` remains on the remote after merge.
