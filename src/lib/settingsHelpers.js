export const toConfigKey = (providerKey) =>
  providerKey
    .replace(/-([a-z])/g, (_, c) => c.toUpperCase())
    .replace('Devops', 'DevOps');

export const createDefaultConnection = (configFields) => ({
  id: crypto.randomUUID(),
  enabled: true,
  ...Object.fromEntries(configFields.map((field) => [field.key, field.defaultValue ?? ''])),
});

export const validateConnection = (connection, configFields, user, providerKey) => {
  const errors = {};

  for (const field of configFields) {
    const value = String(connection[field.key] ?? '').trim();

    if (
      field.key === 'personalAccessToken' &&
      providerKey === 'azure-devops' &&
      user?.provider === 'microsoft'
    ) {
      continue;
    }

    if (field.required && !value) {
      errors[field.key] = `${field.label} is required.`;
      continue;
    }

    if (!value) {
      continue;
    }

    if (field.key === 'baseUrl') {
      try {
        new URL(value);
      } catch {
        errors[field.key] = 'Must be a valid URL (e.g. https://yourorg.atlassian.net).';
      }
    }

    if (field.key === 'email' && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) {
      errors[field.key] = 'Must be a valid email address.';
    }
  }

  return errors;
};

export const buildValidationErrors = (configuration, connectorMetadata, user) => {
  const result = {};
  let hasErrors = false;

  for (const connector of connectorMetadata) {
    const configKey = toConfigKey(connector.providerKey);
    const connections = configuration[configKey] ?? [];
    const connErrors = connections.map((connection) =>
      validateConnection(connection, connector.configFields, user, connector.providerKey),
    );

    result[configKey] = connErrors;
    if (connErrors.some((entry) => Object.keys(entry).length > 0)) {
      hasErrors = true;
    }
  }

  return { errors: result, hasErrors };
};

export const normalizeConfig = (config) => ({
  azureDevOps: config?.azureDevOps ?? [],
  github: config?.github ?? [],
  jira: config?.jira ?? [],
  microsoftTasks: config?.microsoftTasks ?? [],
  outlookFlaggedMail: config?.outlookFlaggedMail ?? [],
  trello: config?.trello ?? [],
  preferences: { orderedItemIds: config?.preferences?.orderedItemIds ?? [] },
});
