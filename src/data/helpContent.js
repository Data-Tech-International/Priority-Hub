export const helpContent = {
  'dashboard.overview': {
    title: 'How priority scoring works',
    body: [
      'Each item is scored: Impact × 4 + Urgency × 3 + Confidence × 1.5 + Age (capped 10) + Blockers × 4 + Due-date weight − Effort × 2.',
      'Scores above 82 → Critical · 60–81 → Focus · below 60 → Maintain.',
      'Drag cards to set one persistent cross-source order. Items that appear for the first time are highlighted until you place them.',
    ],
  },
  'dashboard.filters': {
    title: 'Using filters',
    body: [
      'All filters combine with AND logic — only items matching every active filter are shown.',
      'The search box matches title, ID, and tags. The Tags dropdown lets you narrow to any items that carry at least one of the selected tags.',
      'Filters do not affect drag-drop ordering — the full ordered list is always preserved.',
    ],
  },
  'settings.connectors.azure-devops': {
    title: 'Setting up Azure DevOps',
    body: [
      'Organization is the part of your Azure DevOps URL after dev.azure.com/, e.g. "contoso".',
      'If you sign in with Microsoft, leave the PAT field empty — your session token is forwarded automatically.',
      'The default WIQL fetches all open items in the project. Add AND [System.AssignedTo] = @me to narrow to your own items.',
    ],
  },
  'settings.connectors.jira': {
    title: 'Setting up Jira',
    body: [
      'Base URL is your Jira instance root, e.g. https://yourorg.atlassian.net.',
      'Email must match your Atlassian account. Generate an API token at id.atlassian.com/manage/api-tokens.',
      'JQL controls which issues are fetched. The default returns all open issues assigned to you.',
    ],
  },
  'settings.connectors.trello': {
    title: 'Setting up Trello',
    body: [
      'Board ID is the alphanumeric code from your board URL: trello.com/b/BOARDID/name.',
      'Get your API Key and Token at trello.com/app-key. Click the Token link after receiving your key.',
    ],
  },
  'settings.connectors.github': {
    title: 'Setting up GitHub Issues',
    body: [
      'Owner and Repository should match the target repo, for example owner "microsoft" and repository "vscode".',
      'You can provide a personal access token, or leave it empty when signed in with GitHub OAuth.',
      'Query uses GitHub issue search syntax. The default returns open issues assigned to your account.',
    ],
  },
  'settings.connectors.microsoft-tasks': {
    title: 'Setting up Microsoft Tasks',
    body: [
      'This connector reads Microsoft To Do tasks through your Microsoft sign-in. Re-login after the feature ships so the app can request Tasks.Read consent.',
      'Leave Task list name empty to aggregate tasks from every visible To Do list, or enter an exact list name to narrow the connector.',
      'Linked resources are used when available so task cards can open the original Microsoft item or linked email.',
    ],
  },
  'settings.connectors.outlook-flagged-mail': {
    title: 'Setting up Outlook Flagged Mail',
    body: [
      'This connector reads flagged Outlook messages through Microsoft Graph using your Microsoft sign-in. Re-login after the feature ships so the app can request Mail.Read consent.',
      'Leave Folder ID empty to scan the mailbox feed broadly, or provide a folder ID to narrow the connector to one mail folder.',
      'Max results limits how many flagged messages are surfaced per connector so large mailboxes do not dominate dashboard refresh time.',
    ],
  },
  'settings.export': {
    title: 'About data export',
    body: [
      'Config export downloads your connector settings with PATs and tokens masked.',
      'Dashboard snapshot exports the last loaded work items as JSON for offline use or debugging.',
    ],
  },
};
