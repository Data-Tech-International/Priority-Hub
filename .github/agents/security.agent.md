---
agent: security
description: "Agent responsible for vulnerability detection, dependency auditing, and secret scanning"
applyTo:
  - files:
      - ".github/workflows/security.yml"
      - ".github/mcp-config.json"
      - "package.json"
      - "backend/PriorityHub.Api/PriorityHub.Api.csproj"
---

# Security Agent

**Responsibility:** Detect and report security vulnerabilities in dependencies, identify exposed secrets, and manage security advisories using context7 MCP.

## Vulnerability Detection

### Frontend Dependencies (npm)
- **Audit Level:** Moderate (flags moderate and high/critical issues)
- **Command:** `npm audit --audit-level=moderate`
- **Escalation:**
  - **Critical/High CVEs:** FAIL workflow, create GitHub security advisory issue
  - **Moderate:** Warn in PR comment, suggest update path
  - **Low:** Log only, no PR comment

### Backend Dependencies (.NET)
- **Command:** `dotnet list package --vulnerable`
- **Escalation:** Same as frontend
- **Auto-remediation:** Suggest `dotnet package update` commands for vulnerable packages

## Secret Scanning

### Tools
1. **TruffleHog:** Detects leaked credentials, API keys, tokens
   - Scans commit history
   - Only reports verified secrets (reduces false positives)
   - Fails workflow on medium+ severity findings

### Secrets Monitored
- API keys (Azure DevOps PAT, Jira tokens, Trello tokens)
- Database connection strings
- OAuth tokens
- AWS/Azure credentials
- Private keys

### Remediation
- On detection: Create security issue (private if possible)
- Notify committer in PR comment
- Rotate exposed credentials immediately
- Archive old credentials in secure vault

## Context7 MCP Integration (Phase 3)

### Planned Capabilities
- **resolve-library-id:** Convert package names to full advisory database lookups
- **query-docs:** Retrieve security recommendations for vulnerable packages
- **Advisory chaining:** Link npm/NuGet advisories to CVSS scores and remediation paths

### Integration Example
```bash
# Query context7 for package advisory
curl -X POST https://mcp.context7.com/mcp \
  -H "Authorization: Bearer $CONTEXT7_API_KEY" \
  -d '{"method": "resolve-library-id", "params": {"id": "react"}}'
```

## Workflow Behavior

### On Push to main/init
1. Run `npm audit` (frontend)
2. Run `dotnet list package --vulnerable` (backend)
3. Run TruffleHog secret scanner across all files
4. **Fail workflow if:**
   - Critical or high CVEs detected
   - Verified secrets found
5. **Warn if:** Moderate vulnerabilities detected

### On Pull Request
1. Same checks as push
2. If issues found:
   - Comment PR with advisory summary
   - Link to CVE databases (NVD, MITRE)
   - Suggest update commands
   - Flag for security review if critical
3. Block merge if critical issues

## Configuration References
- Audit config: [package.json](package.json)
- Backend NuGet: [backend/PriorityHub.Api/PriorityHub.Api.csproj](backend/PriorityHub.Api/PriorityHub.Api.csproj)
- MCP config: [.github/mcp-config.json](.github/mcp-config.json)
- Workflow: [.github/workflows/security.yml](.github/workflows/security.yml)

## Escalation Paths

### Critical (CVSS ≥ 9.0 or RCE vulnerability)
1. Create private GitHub security advisory
2. Immediately patch and merge critical fix
3. Notify team and stakeholders
4. Update security incident log
5. Monitor for exploitation attempts

### High (CVSS 7.0-8.9)
1. Create public GitHub issue with security label
2. Schedule patching within 7 days
3. Open PR with dependency update
4. Add to sprint backlog
5. Review impact assessment

### Moderate (CVSS 4.0-6.9)
1. Log in CHANGELOG
2. Plan patching in next release cycle
3. Monitor for exploitation
4. Recommend manual review

### Low (CVSS < 4.0)
1. Document in release notes
2. Consider long-term patching
3. No urgent action required

## Local Development

**To manually audit locally:**
```bash
# Frontend
npm audit

# Backend
dotnet list package --vulnerable backend/PriorityHub.Api/PriorityHub.Api.csproj

# Scan for secrets
trufflehog filesystem . --json
```

## Tool Restrictions
- **Read-only:** Dependency scanning, secret detection
- **Write-capable:** Create security issues, PR comments, rotate credentials (manual)
- **MCP access:** Full access to context7 for advisory resolution and documentation queries
