# Workspace Setup Status

- [x] Verify that the copilot-instructions.md file in the .github directory is created.
- [x] Clarify Project Requirements
- [x] Scaffold the Project
- [x] Customize the Project
- [x] Install Required Extensions
- [x] Compile the Project
- [x] Create and Run Task
- [ ] Launch the Project
- [x] Ensure Documentation is Complete

## Project Notes

- App type: Blazor Server frontend with ASP.NET Core 10 backend-for-frontend.
- Purpose: unify Azure DevOps, Jira, and Trello board work into one personal priority dashboard.
- Current data source: live backend-for-frontend aggregation from configured Azure DevOps, Jira, and Trello connectors.
- Runtime architecture: Blazor Server UI runs on the same ASP.NET Core host, injecting backend services directly via DI.
- Local secret config: `config/providers.local.json` is gitignored and managed through the UI configuration studio.
- Connector model: multiple Azure DevOps, Jira, and Trello connector instances can run in parallel.
- SDK note: this environment has .NET 10 installed and the backend now targets `net10.0`.
- Connector UI: forms now expose only required fields and validate client-side before save.
- Queue behavior: users can persist a manual drag-drop item order across multiple sources and newly seen items are highlighted.
- VS Code setup: no extensions are required by the Vite setup metadata, and debug-style project launch configuration is available.
- Specification: Before creation of implementation plan create specification and ask if in doubt
- Plans: Store all plans in the `plans/` directory with clear step-by-step instructions, file lists, and verification steps. Link to relevant plans in the README and update as needed.
- Testing: backend tests with xUnit and mocked HttpClient; UI component tests with bUnit. Run all tests with `dotnet test PriorityHub.sln`.
- Documentation: keep the README up to date with setup instructions, architecture overview, and usage guide. Add comments in code for complex logic and public methods.
- Git hygiene: ensure all local config files are gitignored, commit with clear messages, and maintain a clean commit history.
- Specification: the README should serve as the single source of truth for how to set up, run, and use the app, while the plans/ directory should contain detailed implementation plans for each major feature or change.
- Spec-first workflow: each major change must start from a GitHub issue labeled `specification`. Implementation begins only after a generated plan exists and the issue has label `plan-approved`.
- Implementation handoff: once plan is approved, automation creates an implementation issue, feature branch, and draft PR. The implementation issue is assigned to both the spec author and `copilot` (GitHub Copilot Coding Agent). The agent reads `AGENTS.md` for project conventions and implements code, tests, and docs on the feature branch.
- Agent setup: `.github/copilot-setup-steps.yml` defines the environment (Node 20, .NET 10) that the Coding Agent uses before starting work.
- Interactive review: use VS Code Copilot Chat Plan mode for spec drafting and plan review. Use `/review` on PRs for self-check before human review.
- Communication: if any part of the requirements or implementation is unclear, ask for clarification before proceeding. It's better to ask questions early than to make incorrect assumptions.
- Verification: after completing each plan, verify that the implemented feature works as expected and update the README with any new setup or usage instructions related to that feature.
- Maintenance: periodically review the README and Plans/ directory to ensure they remain accurate and up to date with the current state of the project. Remove or archive any outdated plans or instructions.
- Security: ensure that no sensitive information (like API keys or secrets) is hardcoded in the codebase or committed to version control. Use environment variables or secure vaults for any secrets needed in development or production.
- Performance: consider the performance implications of the backend aggregation and API design, especially as more connectors or data sources are added. Optimize API responses and connector implementations as needed to maintain a responsive UI experience.
- User experience: ensure that the dashboard UI is intuitive and responsive, with clear feedback for loading states, errors, and new items. Consider accessibility best practices in the design and implementation of the UI components.
- Future features: keep in mind potential future features like additional connectors, user authentication, or advanced filtering/sorting options when designing the architecture and API, to allow for easier expansion down the line.
- Collaboration: if working with others, ensure that the codebase is well-documented and that plans are clearly communicated to all team members. Use code reviews and regular check-ins to maintain alignment on implementation details and project goals.

## Verified Commands

- Start dev server: npm run dev
- Build: npm run build
- Run all tests: npm run test
- Build solution directly: dotnet build PriorityHub.sln
- Test solution directly: dotnet test PriorityHub.sln

## Current Status

- The frontend is a Blazor Server app in `backend/PriorityHub.Ui/`.
- The backend API is implemented in ASP.NET Core in `backend/PriorityHub.Api/`.
- UI components inject backend services directly via DI (no HTTP boundary between UI and backend).
- Configuration is managed through the Settings page and persisted into the local provider config file.
- VS Code task and debug files exist for Blazor development.
- Debug-style project launch configuration is available and can be started explicitly by the user.