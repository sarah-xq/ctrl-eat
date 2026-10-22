---
name: ctrl-eat-ai-layer-evolution
description: Review a Ctrl+Eat coding task after an agent mistake, repeated friction, or a request to improve the project workflow. Determine whether an evidence-based change to project AI rules or skills is justified, and propose a focused patch for review.
---

# Ctrl+Eat AI Layer Evolution

Use after the immediate issue has been understood or fixed. Improve reusable guidance only when the evidence points to a repeatable process gap. A review does not need to produce a new rule.

## Review

1. **Establish what happened.** Describe the expected behavior, observed failure or friction, and supporting evidence from the task instructions, conversation, diff, or tests. Distinguish confirmed causes from hypotheses. Consider code defects, unclear requirements, missing tests, tooling limitations, and guidance gaps; these may overlap.

2. **Inspect the relevant guidance.** Read the instruction or skill files that should have governed the task. Determine whether guidance was missing, unclear, conflicting, difficult to discover, or available but not followed. Do not add a duplicate rule to address failure to follow an existing one. Keep inspection scoped to the incident.

3. **Assess recurrence.** Identify a concrete future Ctrl+Eat task where the same failure mechanism could recur. Repeated incidents strengthen the case, but one well-understood incident can justify a narrow improvement. Do not turn a one-off mistake, temporary workaround, or local requirement into a broad rule. Ask the user only when missing context materially affects the assessment.

4. **Choose the appropriate intervention.** Consider whether a regression test, validation, tooling change, or requirement clarification would address the cause more reliably. Propose guidance when it would improve a future agent decision that those controls do not adequately address. Keep the immediate code fix and any other recommendations separate from the guidance proposal.

5. **Prepare the smallest useful change.** Identify the narrowest relevant instruction or skill file. Prefer clarifying, consolidating, relocating, or removing existing guidance before adding a new rule. Preserve one authoritative location. State when the rule applies and what action it changes; include exceptions only when they prevent a likely misapplication.

6. **Check the proposed behavior.** Replay the original incident with the proposed wording: would it have changed the relevant decision? Check a nearby legitimate task where the rule should not apply or should allow a different approach. Revise wording that introduces unnecessary work, permissions, or restrictions. Distinguish this reasoning check from any testing actually performed.

7. **Present the proposal for review.** Explain what the change should prevent, its applicability, and its trade-off. Provide the concrete patch before requesting approval. Apply only the reviewed change once the user approves it; existing approval for that exact change does not need to be requested again.

## Constraints

- Ground recommendations in available evidence. Do not invent a cause, recurring incident, file path, or existing instruction.
- If essential evidence is unavailable, identify the smallest missing input rather than expanding into an open-ended investigation.
- Keep project-specific lessons within the relevant project or workflow.
- Do not change unrelated skills, introduce an agent framework, or automate approvals, merges, or deployments.
- Do not add rules merely to restate guidance already enforced elsewhere.
- Do not edit project guidance before the user has reviewed and approved the concrete change.
- After an approved edit, verify that the resulting diff matches the proposal and that any affected references remain valid. Report what was checked without claiming that wording validation proves future effectiveness.

## Handoff

Return one of:

### No reusable change recommended

State the supporting evidence and a short reason. If appropriate, identify a separate code, test, tooling, or requirement fix without treating it as a guidance change.

### Insufficient evidence

State what is known, the unresolved question, and the smallest additional evidence needed to decide. Do not present uncertainty as proof that no improvement is needed.

### Proposed guidance patch

Include:

- **Target:** the verified file path and relevant section.
- **Evidence:** the observed failure and supported cause.
- **Recurrence:** a concrete future task exposed to the same failure mechanism.
- **Patch:** the exact focused diff.
- **Behavior check:** how it would affect the original incident and a nearby legitimate case.
- **Trade-off:** expected benefit, scope, and any added maintenance or workflow cost.

If the target file cannot be inspected, provide exact proposed wording labeled as a draft, and state that its placement remains unverified.

This is the project’s improvement loop: understand and fix the work, then make a small, evidence-based adjustment to the AI layer when it will help future tasks.
