# Plan: Specification-First Agent Workflow

## Status: COMPLETED

## Goal

Enforce a repository workflow where each major change starts with a specification, then a proposed plan, then an automated implementation bootstrap (issue, branch, draft PR).

## Files

- `.github/ISSUE_TEMPLATE/specification.yml`
- `.github/workflows/spec-intake.yml`
- `.github/workflows/spec-plan.yml`
- `.github/workflows/spec-implementation-bootstrap.yml`
- `.github/agents/spec-planner.agent.md`
- `.github/mcp-config.json`
- `.github/copilot-instructions.md`
- `README.md`

## Steps

1. Add a GitHub issue template dedicated to specification intake.
2. Add a workflow that persists specification issues into `plans/specifications/`.
3. Add a workflow that creates plan files in `plans/` from specification issues.
4. Add a workflow that reacts to `plan-approved` by creating implementation issue, branch, and draft PR.
5. Add/extend agent metadata so planning and implementation orchestration are explicit.
6. Update developer documentation with exact lifecycle and labels.

## Labels Used

- `specification`
- `needs-plan`
- `plan-proposed`
- `plan-approved`
- `implementation`
- `agent-ready`
- `implementation-started`

## Verification

1. Create issue using `Specification Request` template and confirm label `specification`.
2. Confirm `Spec Intake` workflow runs and creates a markdown file in `plans/specifications/`.
3. Confirm issue receives label `needs-plan`.
4. Confirm `Spec Plan Proposal` workflow runs and creates `plans/spec-<id>-<slug>-plan.md`.
5. Confirm issue label changes from `needs-plan` to `plan-proposed`.
6. Add label `plan-approved` to the same issue.
7. Confirm bootstrap workflow creates:
   - implementation issue,
   - feature branch `feature/spec-<id>-<slug>`,
   - draft PR to `main`.
8. Confirm implementation issue includes checklist for code, tests, and docs.
