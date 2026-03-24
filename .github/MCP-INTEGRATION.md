---
title: MCP Integration for CI/CD Agents
description: How GitHub CI agents use Model Context Protocol (MCP) servers for enhanced security, analysis, and testing capabilities
---

# MCP Integration for Priority Hub CI/CD

## Overview

The Priority Hub CI/CD pipeline integrates two MCP (Model Context Protocol) servers to enhance automated quality checks:

1. **context7** — Security advisory lookups, library resolution, best practices documentation
2. **github** — Issue/PR automation for agent feedback and escalation

## Server Configurations

### context7 MCP Server

**Type:** HTTP-based security and documentation service  
**Endpoint:** `https://mcp.context7.com/mcp`  
**Authentication:** Bearer token (stored in `CONTEXT7_API_KEY` secret)

#### Available Tools

| Tool | Input | Output | Use Case |
|------|-------|--------|----------|
| `query-docs` | Query string | Documentation results | Find best practices, patterns, architecture guides |
| `resolve-library-id` | Package name (npm/NuGet) | Library metadata, advisories | Link dependencies to CVE databases |
| `check-security-advisory` | Library ID + version | Advisory severity, CVSS score | Determine vulnerability impact |
| `get-remediation-path` | Vulnerable library | Update sequence, breaking changes | Guide dependency updates |

### github MCP Server

**Type:** Built-in GitHub Actions integration  
**Scope:** Current repository

#### Available Tools

| Tool | Input | Output | Use Case |
|------|-------|--------|----------|
| `create-issue` | Title, body, labels | Issue URL | Escalate findings to backlog |
| `create-pull-request` | Branch, title, body | PR URL | Auto-generate fixes (Phase 2+) |
| `add-pr-comment` | PR number, comment text | Comment URL | Provide inline feedback |
| `list-workflows` | None | Workflow list | Monitor CI status |

## Agent MCP Usage

### Security Agent

**Required:** context7  
**Flow:**
```
1. Run npm audit + dotnet list package --vulnerable
2. For each vulnerable library:
   a. security.resolve-library-id(package_name)
   b. security.check-security-advisory(lib_id, version)
   c. Get remediation path via security.get-remediation-path()
3. Format findings as PR comment via github.add-pr-comment()
4. If critical CVE: github.create-issue() with security label
```

**Example (context7 call within workflow):**
```yaml
- name: Check npm package advisories
  env:
    CONTEXT7_API_KEY: ${{ secrets.CONTEXT7_API_KEY }}
  run: |
    # npm audit finds vulnerable package "lodash@4.17.20"
    # Agent calls context7:
    curl -X POST https://mcp.context7.com/mcp \
      -H "Authorization: Bearer $CONTEXT7_API_KEY" \
      -d '{
        "method": "check-security-advisory",
        "params": {
          "library_id": "npm:lodash",
          "version": "4.17.20"
        }
      }'
    # Returns: { severity: "high", cvss: "7.5", remediation: "update to 4.17.21+" }
```

### Static Analysis Agent

**Optional:** context7  
**Flow:**
```
1. Run backend build with Roslyn analyzers
2. Analyze code complexity metrics
3. On architectural violations:
   a. query-docs() for design patterns
   b. Provide suggestion in PR via add-pr-comment()
4. On high complexity:
   a. Link to architecture documentation
   b. Suggest refactoring patterns from docs
```

### Test Coverage Agent

**Optional:** context7  
**Flow:**
```
1. Run tests, collect coverage
2. If coverage < 60% but improving:
   a. query-docs("testing best practices for [technology]")
   b. Add comment with testing tips via add-pr-comment()
3. Artifact storage via GitHub Actions (no MCP needed)
```

### Implementation Orchestrator Agent

**Required:** github  
**Flow:**
```
1. Detect spec issue with plan-approved label
2. Create implementation issue referencing spec and plan
3. Create feature branch: feat/<issue-number>-<short-slug>
4. Create draft PR via github.create-pull-request()
5. Assign implementation issue to spec author + copilot
6. Add PR comment via github.add-pr-comment() confirming handoff
```

---

## Setup

### 1. Configure Secrets in GitHub

Add the following to your GitHub repository settings:  
*Settings → Secrets and variables → Actions*

| Secret | Value | Notes |
|--------|-------|-------|
| `CONTEXT7_API_KEY` | `ctx7sk-36944a34-e3c8-49a1-8581-aab2af476e9e` | From `.github/mcp-config.json` |

### 2. Verify MCP Configuration

**File:** [.github/mcp-config.json](.github/mcp-config.json)

The configuration defines:
- Which MCP servers are available
- Which tools each server exposes
- Which agents require or can optionally use each server

### 3. Environment Variables in Workflows

Workflows access context7 via:
```yaml
env:
  CONTEXT7_API_KEY: ${{ secrets.CONTEXT7_API_KEY }}
```

GitHub tools are auto-available in `actions/github-script@v7`.

### 4. Local Development (VS Code)

The GitHub MCP server is configured in `.vscode/mcp.json` for local Copilot Chat use.

**Prerequisites:**
- Node.js 20+ installed (for `npx`)
- A GitHub Personal Access Token with these scopes:
  - `repo` — full repository access
  - `issues:write` — create and update issues
  - `pull_requests:write` — create and update PRs

**How it works:**
- VS Code prompts for your GitHub PAT at runtime via a secure input dialog
- The PAT is never stored in config files or committed to version control
- The server runs as a stdio process using `@modelcontextprotocol/server-github`

**Verification:**
1. Open VS Code in the PriorityHub workspace
2. Open Copilot Chat and check the MCP server list — `github` should appear
3. Start the server — you will be prompted for your PAT
4. Test with a simple query that uses GitHub context

---

## Examples

### Example 1: Detecting a Critical CVE

**Scenario:** A new CVE is published for a dependency in package.json

**Agent Flow:**
1. Security workflow runs `npm audit`, finds vulnerability
2. Security agent calls `context7.check-security-advisory()`
3. Returns CVSS 9.8 (critical)
4. Agent calls `github.create-issue()`:
   ```
   Title: "🔴 CRITICAL: CVE-2024-xxxxx in package-name"
   Label: ["security", "high-priority"]
   Body: "Details, remediation, and link to NVD"
   ```
5. Workflow fails (`exit 1`), blocking merge
6. Team responds to issue, updates dependency, re-tests

### Example 2: Code Complexity Warning

**Scenario:** PR adds a method with 25 cyclomatic complexity

**Agent Flow:**
1. Static-analysis workflow detects high complexity
2. Agent calls `context7.query-docs("refactoring patterns for complex functions")`
3. Adds PR comment:
   ```
   ⚠️ Method X has complexity 25 (max recommended: 10)
   
   Suggested refactoring patterns:
   - Extract switch/if statement logic to separate functions
   - Apply Strategy or Chain of Responsibility pattern
   
   Learn more: [Architecture Docs](link-from-context7)
   ```
4. Developer can be required to refactor before merge (configurable)

### Example 3: Improved Test Coverage Suggestion

**Scenario:** PR adds code but coverage stays at 65%

**Agent Flow:**
1. Coverage workflow runs tests, measures coverage delta
2. Agent calls `context7.query-docs("React unit testing best practices")`
3. Adds PR comment:
   ```
   ✅ Coverage meets 60% threshold (current: 65%)
   
   💡 Consider adding tests for:
   - Error boundary scenarios
   - Async loading states
   - Conditional renders
   
   Best practices: [From testing docs]
   ```
4. Developer can add more tests to improve from 65% → 70%

---

## Troubleshooting

### MCP Server Unreachable

**Symptom:** Workflow logs show `curl: (7) Failed to connect to mcp.context7.com`

**Diagnosis:**
- context7 service is down
- Network/firewall blocking HTTPS connection
- Invalid `CONTEXT7_API_KEY`

**Resolution:**
1. Check context7 status page
2. Verify API key in `CONTEXT7_API_KEY` secret
3. Workflow continues with degraded functionality (fallback to basic checks)

### Invalid Library ID

**Symptom:** context7 returns 404 for `resolve-library-id()`

**Diagnosis:**
- Package name doesn't exist in context7 index
- Package is private/internal

**Resolution:**
- Agent skips context7 lookup for unrecognized packages
- Basic `npm audit` / `dotnet list package` warnings still show

### PR Comments Not Appearing

**Symptom:** GitHub tool calls fail in workflow

**Diagnosis:**
- `GITHUB_TOKEN` permissions insufficient
- Workflow has `permissions: read-only`

**Resolution:**
```yaml
# In workflow file:
permissions:
  pull-requests: write
  issues: write
```

---

## Future Enhancements

1. **Phase 3b:** Add more context7 tools:
   - `auto-suggest-fix` for common violations
   - `fetch-changelog` for dependency updates
   - `predict-risk` for ML-based security scoring

2. **Phase 4:** Add custom MCP server for Priority Hub:
   - `validate-connector-config` 
   - `check-api-compatibility`
   - `audit-provider-credentials`

3. **Reporting:** MCP-backed dashboard showing:
   - Security trend over time
   - Code quality metrics
   - Test coverage by module
   - Dependency update recommendations

---

## See Also

- [Agent Definitions](.github/agents/) — Detailed agent behaviors
- [GitHub Workflows](.github/workflows/) — CI/CD pipeline definitions
- [context7 Documentation](https://mcp.context7.com/docs) — Full API reference
