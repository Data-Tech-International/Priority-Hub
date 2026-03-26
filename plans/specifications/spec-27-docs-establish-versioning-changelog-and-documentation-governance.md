# Specification: docs: establish versioning, changelog, and documentation governance

## Metadata
- Source issue: #27
- Source URL: https://github.com/Data-Tech-International/Priority-Hub/issues/27
- Author: @ipavlovi
- Created: 2026-03-26T11:04:02Z

## Specification

## Background
Priority Hub needs explicit governance for Semantic Versioning, changelog hygiene, and markdown documentation structure to prevent drift and improve consistency.

Scope
Add mandatory instruction rules for Semantic Versioning, changelog maintenance, and markdown user documentation.
Create baseline assets:
Directory.Build.props for .NET version alignment
CHANGELOG.md in Keep a Changelog format
docs/ markdown skeleton
README updates as docs entry point
Update instruction files:
AGENTS.md
copilot-instructions.md
Acceptance Criteria
 Semantic Versioning rules are mandatory in instruction files.
 Version synchronization is required between package.json and Directory.Build.props.
 CHANGELOG.md exists and follows Keep a Changelog format.
 Unreleased entries are required for behavior changes.
 docs/ contains markdown structure for features, configuration, processes, and troubleshooting.
 README.md is the entry point linking to docs/.
 Instruction files are aligned and non-duplicative.
Out of Scope
CI automation for release/version/changelog checks.
Full historical backfill of all existing docs content.
Verification
dotnet build PriorityHub.sln
dotnet test PriorityHub.sln
Manual verification of links and governance text in:
AGENTS.md
copilot-instructions.md
README.md
CHANGELOG.md
docs/

## Clarifications
- [ ] Confirm assumptions before planning if anything is unclear.
