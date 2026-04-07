# Specification: spec: UI onboarding redirect, import auto-save, and collapsible dashboard panels

## Metadata
- Source issue: #70
- Source URL: https://github.com/Data-Tech-International/Priority-Hub/issues/70
- Author: @ipavlovi
- Created: 2026-04-07T08:28:35Z

## Specification

## Summary

Three targeted UI improvements to Priority Hub:

1. **First-run onboarding redirect** — When an authenticated user has no connectors configured, automatically redirect from the dashboard to the Settings page with a toast message guiding them to configure their first connector.
2. **Import auto-save and tab switch** — After a configuration import is confirmed, automatically persist the imported settings and navigate to the Connectors tab.
3. **Collapsible dashboard sections** — Make the hero panel (title, description, metrics) and the title/filter panel collapsible so power users can maximize the work-item list area.

## Specification

See [`plans/specifications/spec-design-ui-onboarding-import-autosave-collapsible-panels.md`](https://github.com/Data-Tech-International/Priority-Hub/blob/spec/ui-onboarding-import-autosave-collapsible-panels/plans/specifications/spec-design-ui-onboarding-import-autosave-collapsible-panels.md)

## Branch

`spec/ui-onboarding-import-autosave-collapsible-panels`

## Acceptance Criteria

### Feature 1: First-Run Redirect
- [ ] Empty config → redirect to `/settings?onboarding=true`
- [ ] Onboarding toast displayed on Settings page
- [ ] Users with connectors → no redirect
- [ ] No redirect loop on back-navigation

### Feature 2: Import Auto-Save
- [ ] Confirm import → auto-save + switch to Connectors tab
- [ ] Success banner: "Configuration imported and saved. Review your connectors below."
- [ ] Save failure → stay on Import/Export tab with error
- [ ] Manual save flow unchanged

### Feature 3: Collapsible Panels
- [ ] Hero panel collapsible with toggle button
- [ ] Filters panel collapsible with toggle button
- [ ] Collapse state persisted in localStorage
- [ ] Default state is expanded
- [ ] Collapsed filters don't clear active filter state
- [ ] `aria-expanded` attribute on toggle buttons


## Clarifications
- [ ] Confirm assumptions before planning if anything is unclear.
