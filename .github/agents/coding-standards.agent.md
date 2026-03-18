---
agent: coding-standards
description: "Agent responsible for enforcing coding standards via linting and formatting checks"
applyTo:
  - files:
      - ".github/workflows/coding-standards.yml"
      - ".eslintrc.json"
      - "backend/stylecop.json"
---

# Coding Standards Agent

**Responsibility:** Enforce consistent code style and formatting across the frontend (JavaScript/React) and backend (.NET/C#) codebases.

## Frontend (JavaScript/React)

### Rules
- **ESLint violations** are reported as warnings if style-only, errors if affecting correctness
- **Console usage:** Only `console.error()` and `console.warn()` are allowed; `console.log()` and `console.info()` trigger warnings
- **Unused variables:** Flagged unless prefixed with `_` (e.g., `_unused`)
- **Equality:** Strict equality (`===`) required; loose equality (`==`) is an error
- **Indentation:** 2 spaces (enforced)
- **Quotes:** Single quotes preferred with escape exception allowed
- **Semicolons:** Required at statement ends
- **Arrow functions and async:** Proper usage enforced by ESLint rules

### Auto-Fix Capability
- On PR: Agent comments with suggestion: *"Run `npm run lint:fix` locally to auto-fix style issues"*
- Can be invoked via PR comment: `@copilot fix-style`

## Backend (.NET/C#)

### Rules (StyleCop + dotnet format)
- **Indentation:** 4 spaces (configured in `stylecop.json`)
- **Naming conventions:** PascalCase for public members, camelCase for private/internal
- **Private fields:** May start with `_`
- **Documentation:** Required for public classes, methods, and interfaces
- **Ordering:** Using statements grouped, then properties, then constructors, then methods
- **Max line length:** 120 characters (soft limit in StyleCop)
- **Spacing:** Around operators and after keywords

### Auto-Fix Capability
- On PR: Agent comments: *"Run `dotnet format backend/PriorityHub.Api/PriorityHub.Api.csproj` locally to fix"*
- Can be invoked via PR comment: `@copilot fix-format`

## Workflow Behavior

### On Push to main/init
1. Run ESLint on frontend
2. Run `dotnet format --verify-no-changes` on backend
3. Fail if errors found (non-style issues)
4. Warn if only style violations

### On Pull Request
1. Same checks as push
2. If violations exist:
   - Comment on PR with findings
   - Provide auto-fix command suggestions
   - Mark as ready for review once fixed
3. **Required status:** Can be configured as required before merge (optional)

## Configuration References
- Frontend: [.eslintrc.json](.eslintrc.json)
- Backend: [backend/stylecop.json](backend/stylecop.json)
- Workflow: [.github/workflows/coding-standards.yml](.github/workflows/coding-standards.yml)

## Escalation
- **Critical:** Readability or maintainability issue → Create GitHub issue with "code-style" label
- **High:** Breaking convention in main branch → Create PR with fixes automatically
- **Medium:** Style inconsistency → Comment on PR with suggestions
