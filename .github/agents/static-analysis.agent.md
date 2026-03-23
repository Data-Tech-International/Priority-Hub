---
name: static-analysis
description: "Agent responsible for code quality metrics, complexity analysis, and static code inspection"
target: vscode
---

# Static Code Analysis Agent

**Responsibility:** Measure and enforce code quality through static analysis, complexity metrics, and architectural compliance checks.

## Backend Code Analysis

### Tools
1. **Roslyn Analyzers:** Real-time semantic analysis during build
   - Category: Design, Maintainability, Naming, Performance, Reliability, Security, Usage
   - Severity levels: Error, Warning, Info, Hidden

2. **StyleCop Analyzers:** Code style and ordering rules
   - Configuration: [backend/stylecop.json](../../backend/stylecop.json)
   - Errors: Fail build if high severity
   - Warnings: Report but don't fail

3. **Code Metrics** (calculated during build):
   - **Cyclomatic Complexity:** Max 10 per method (warn), 15 (error)
   - **Lines of Code:** Max 300 per class (warn), 500 (error)
   - **Nesting Depth:** Max 4 levels (warn)
   - **Method Count:** Max 20 per class (warn)

### Execution
```bash
dotnet build backend/PriorityHub.Api/PriorityHub.Api.csproj \
  --no-restore \
  /p:EnforceCodeStyleInBuild=true \
  /p:TreatWarningsAsErrors=false
```

**Fail criteria:**
- Critical errors in Roslyn analysis
- Design pattern violations (e.g., improper async usage, null safety)

**Warn criteria:**
- Code style violations
- Complexity warnings
- Maintainability issues

## Frontend Code Analysis

### Checks
1. **Console Usage:** Only `console.error()` and `console.warn()` allowed
   - Detected via regex: `console\.(log|info|debug|trace)`
   - Severity: Warning

2. **Unused Dependencies:** Identify packages in `package.json` not imported
   - Tool: `npm ls --depth=0` + semantic analysis
   - Severity: Info (no fail)

3. **Code Complexity:**
   - Function length: Max 50 lines (warn), 100 (error)
   - Nesting depth: Max 3 levels (warn)
   - File size: Max 300 lines (warn)

### Execution
```bash
npm install
npm run lint  # Detects console, style, and basic complexity
grep -r "console\." src/ --include="*.jsx" --include="*.js"
```

## Architectural Compliance

### Patterns Enforced

**Frontend (React/Vite)**
- Components in `src/components/`
- Pages in `src/pages/`
- Utilities in `src/lib/`
- No direct API calls in components (use `src/lib/api.js`)
- No hardcoded secrets or config in code

**Backend (ASP.NET Core)**
- Controllers in `backend/PriorityHub.Api/`
- Services in `Services/`
- Models in `Models/`
- Tests mirrored in `PriorityHub.Api.Tests/`
- Dependency injection for external services
- No direct file I/O outside `LocalConfigStore`

### Violation Severity
- **Error:** Secrets in code, hardcoded API endpoints, direct DB access in controller
- **Warning:** Code in wrong folder, missing abstraction layer
- **Info:** Optional improvements

## Workflow Behavior

### On Push to main/init
1. Build backend with Roslyn analyzers → Fail if critical errors
2. Check frontend console usage → Warn if violations
3. Generate code metrics → Report complexity scores
4. Compile analysis results → Create summary report
5. **Status:** ✅ Pass if no critical issues, ⚠️ Warn if only style/metrics

### On Pull Request
1. Same checks as push
2. If violations:
   - Post comment with findings
   - Link to relevant architectural docs
   - Suggest refactoring approaches if complexity exceeded
3. Can comment with: `@copilot suggest-refactor` for suggestions

## Configuration References
- Backend build: [backend/PriorityHub.Api/PriorityHub.Api.csproj](../../backend/PriorityHub.Api/PriorityHub.Api.csproj)
- StyleCop rules: [backend/stylecop.json](../../backend/stylecop.json)
- ESLint rules: [.eslintrc.json](../../.eslintrc.json)
- Workflow: [.github/workflows/static-analysis.yml](../workflows/static-analysis.yml)

## Metrics Dashboard

**Calculated and reported:**
```
Backend Code Metrics
  - Total namespace count: {count}
  - Average complexity: {value}
  - Max method complexity: {method_name} = {value}
  - LOC per class (avg): {value}
  - Dead code detected: {file_list}

Frontend Code Metrics
  - File count: {count}
  - Console violations: {count}
  - Unused packages: {list}
  - Estimated complexity: {score}
```

## Local Development

**To run locally:**
```bash
# Backend analysis
dotnet build backend/PriorityHub.Api/PriorityHub.Api.csproj \
  /p:EnforceCodeStyleInBuild=true

# Frontend analysis
npm run lint  # All checks
grep -r "console\." src/  # Console only
```

## Escalation

### Critical Issues
- **Design violation in main branch:** Create security/refactor issue with "tech-debt" label
- **New violation in PR:** Block merge until resolved
- **Persistent high complexity:** Schedule refactor sprint, add to backlog

### High Priority
- **Complexity threshold exceeded:** Suggest breaking into smaller functions
- **Code duplication:** Identify and extract common logic
- **Missing abstractions:** Add to code review comments

### Medium Priority
- **Style inconsistencies:** Log for next cleanup pass
- **Unused dependencies:** Investigate and remove if safe
- **Metrics trending up:** Monitor in trend report

## Tool Restrictions
- **Read-only:** Analyze code, detect violations
- **Write-capable:** PR comments, create issues, link to docs
- **MCP access:** Query dependencies and architectural patterns (Phase 3 integration)
