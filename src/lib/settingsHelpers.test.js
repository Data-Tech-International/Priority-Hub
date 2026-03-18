import { describe, expect, it } from 'vitest';
import {
  buildValidationErrors,
  normalizeConfig,
  toConfigKey,
  validateConnection,
} from './settingsHelpers';

describe('toConfigKey', () => {
  it('converts provider keys to camelCase config keys', () => {
    expect(toConfigKey('azure-devops')).toBe('azureDevOps');
    expect(toConfigKey('microsoft-tasks')).toBe('microsoftTasks');
    expect(toConfigKey('outlook-flagged-mail')).toBe('outlookFlaggedMail');
    expect(toConfigKey('github')).toBe('github');
  });
});

describe('normalizeConfig', () => {
  it('fills missing arrays and preferences with defaults', () => {
    const normalized = normalizeConfig({
      azureDevOps: null,
      github: [{ id: 'g1' }],
      preferences: null,
    });

    expect(normalized).toEqual({
      azureDevOps: [],
      github: [{ id: 'g1' }],
      jira: [],
      microsoftTasks: [],
      outlookFlaggedMail: [],
      trello: [],
      preferences: { orderedItemIds: [] },
    });
  });
});

describe('validateConnection', () => {
  it('returns required field error when value is blank', () => {
    const fields = [{ key: 'name', label: 'Connection name', required: true }];
    const errors = validateConnection({ name: '   ' }, fields, null, 'jira');
    expect(errors).toEqual({ name: 'Connection name is required.' });
  });

  it('accepts valid baseUrl and flags invalid baseUrl', () => {
    const fields = [{ key: 'baseUrl', label: 'Base URL', required: true }];
    const validErrors = validateConnection({ baseUrl: 'https://yourorg.atlassian.net' }, fields, null, 'jira');
    const invalidErrors = validateConnection({ baseUrl: 'not-a-url' }, fields, null, 'jira');

    expect(validErrors).toEqual({});
    expect(invalidErrors).toEqual({
      baseUrl: 'Must be a valid URL (e.g. https://yourorg.atlassian.net).',
    });
  });

  it('accepts valid email and flags invalid email', () => {
    const fields = [{ key: 'email', label: 'Email', required: true }];
    const validErrors = validateConnection({ email: 'me@example.com' }, fields, null, 'jira');
    const invalidErrors = validateConnection({ email: 'invalid-email' }, fields, null, 'jira');

    expect(validErrors).toEqual({});
    expect(invalidErrors).toEqual({ email: 'Must be a valid email address.' });
  });

  it('skips Azure DevOps PAT requirement when signed in with Microsoft', () => {
    const fields = [{ key: 'personalAccessToken', label: 'PAT', required: true }];
    const errors = validateConnection(
      { personalAccessToken: '' },
      fields,
      { provider: 'microsoft' },
      'azure-devops',
    );

    expect(errors).toEqual({});
  });
});

describe('buildValidationErrors', () => {
  it('returns hasErrors=true when any connection has validation errors', () => {
    const configuration = {
      jira: [{ name: 'Jira', email: 'not-an-email' }],
    };

    const connectorMetadata = [
      {
        providerKey: 'jira',
        configFields: [
          { key: 'name', label: 'Name', required: true },
          { key: 'email', label: 'Email', required: true },
        ],
      },
    ];

    const result = buildValidationErrors(configuration, connectorMetadata, null);

    expect(result.hasErrors).toBe(true);
    expect(result.errors.jira[0].email).toBe('Must be a valid email address.');
  });
});
