import { useEffect, useState } from 'react';
import { useAuth } from '../contexts/AuthContext.jsx';
import { HelpPanel } from '../components/HelpPanel.jsx';
import {
  UnauthorizedError,
  fetchConfiguration,
  fetchConnectorMetadata,
  saveConfiguration,
} from '../lib/api.js';
import { formatProviderName } from '../lib/priorities.js';
import {
  buildValidationErrors,
  createDefaultConnection,
  normalizeConfig,
  toConfigKey,
} from '../lib/settingsHelpers.js';
import { helpContent } from '../data/helpContent.js';

const maskSecret = (value) =>
  value && value.trim() ? `${'*'.repeat(Math.min(value.length, 8))}` : '';

const emptyConfig = {
  azureDevOps: [],
  github: [],
  jira: [],
  microsoftTasks: [],
  outlookFlaggedMail: [],
  trello: [],
  preferences: { orderedItemIds: [] },
};

export default function SettingsPage() {
  const { user, handleUnauthorized, handleSignOut } = useAuth();
  const [activeTab, setActiveTab] = useState('connectors');
  const [connectorMetadata, setConnectorMetadata] = useState([]);
  const [configuration, setConfiguration] = useState(emptyConfig);
  const [validationErrors, setValidationErrors] = useState({});
  const [loadingConfiguration, setLoadingConfiguration] = useState(true);
  const [savingConfiguration, setSavingConfiguration] = useState(false);
  const [statusMessage, setStatusMessage] = useState(null);
  const [signingOut, setSigningOut] = useState(false);

  const loadConnectorMetadata = async () => {
    try {
      const meta = await fetchConnectorMetadata();
      setConnectorMetadata(meta ?? []);
    } catch {
      // Non-fatal: fall back to empty metadata
    }
  };

  const loadConfiguration = async () => {
    setLoadingConfiguration(true);
    try {
      const config = normalizeConfig(await fetchConfiguration());
      setConfiguration(config);
    } catch (err) {
      if (err instanceof UnauthorizedError) {
        handleUnauthorized();
        return;
      }
      setStatusMessage(err instanceof Error ? err.message : 'Could not load configuration.');
    } finally {
      setLoadingConfiguration(false);
    }
  };

  useEffect(() => {
    void Promise.all([loadConnectorMetadata(), loadConfiguration()]);
  }, []);

  const updateConnection = (configKey, index, field, value) => {
    setConfiguration((prev) => ({
      ...prev,
      [configKey]: prev[configKey].map((conn, i) => (i === index ? { ...conn, [field]: value } : conn)),
    }));
    setValidationErrors((prev) => ({
      ...prev,
      [configKey]: (prev[configKey] ?? []).map((connErrors, i) => {
        if (i !== index) return connErrors;
        const next = { ...connErrors };
        delete next[field];
        return next;
      }),
    }));
  };

  const addConnection = (providerKey, configFields) => {
    const configKey = toConfigKey(providerKey);
    setConfiguration((prev) => ({
      ...prev,
      [configKey]: [...(prev[configKey] ?? []), createDefaultConnection(configFields)],
    }));
    setValidationErrors((prev) => ({
      ...prev,
      [configKey]: [...(prev[configKey] ?? []), {}],
    }));
  };

  const removeConnection = (configKey, index) => {
    setConfiguration((prev) => ({
      ...prev,
      [configKey]: prev[configKey].filter((_, i) => i !== index),
    }));
    setValidationErrors((prev) => ({
      ...prev,
      [configKey]: (prev[configKey] ?? []).filter((_, i) => i !== index),
    }));
  };

  const handleSaveConfiguration = async () => {
    const { errors, hasErrors } = buildValidationErrors(configuration, connectorMetadata, user);
    setValidationErrors(errors);

    if (hasErrors) {
      setStatusMessage('Fix the highlighted fields before saving.');
      return;
    }

    setSavingConfiguration(true);
    setStatusMessage(null);

    try {
      const saved = normalizeConfig(await saveConfiguration({ ...configuration, preferences: { orderedItemIds: configuration.preferences?.orderedItemIds ?? [] } }));
      setConfiguration(saved);
      setStatusMessage('Configuration saved. Return to the Dashboard to see refreshed data.');
    } catch (err) {
      if (err instanceof UnauthorizedError) {
        handleUnauthorized();
        return;
      }
      setStatusMessage(err instanceof Error ? err.message : 'Could not save configuration.');
    } finally {
      setSavingConfiguration(false);
    }
  };

  const handleExportConfig = () => {
    const sanitized = {
      ...configuration,
      azureDevOps: configuration.azureDevOps.map((c) => ({ ...c, personalAccessToken: maskSecret(c.personalAccessToken) })),
      github: (configuration.github ?? []).map((c) => ({ ...c, personalAccessToken: maskSecret(c.personalAccessToken) })),
      jira: configuration.jira.map((c) => ({ ...c, apiToken: maskSecret(c.apiToken) })),
      trello: configuration.trello.map((c) => ({ ...c, apiKey: maskSecret(c.apiKey), token: maskSecret(c.token) })),
    };
    const blob = new Blob([JSON.stringify(sanitized, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `priority-hub-config-${new Date().toISOString().slice(0, 10)}.json`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const handleSignOutClick = async () => {
    setSigningOut(true);
    try {
      await handleSignOut();
    } finally {
      setSigningOut(false);
    }
  };

  return (
    <div className="app-shell">
      <div className="settings-page">
        <div className="settings-header">
          <p className="eyebrow">Priority Hub</p>
          <h1>Settings</h1>
          <p>Manage connector configuration, account, and data export.</p>
        </div>

        {statusMessage ? (
          <section className="status-banner status-info">
            <strong>Status</strong>
            <span>{statusMessage}</span>
          </section>
        ) : null}

        <div className="settings-tabs">
          {['connectors', 'account', 'export'].map((tab) => (
            <button
              key={tab}
              type="button"
              className={`settings-tab${activeTab === tab ? ' is-active' : ''}`}
              onClick={() => setActiveTab(tab)}
            >
              {tab.charAt(0).toUpperCase() + tab.slice(1)}
            </button>
          ))}
        </div>

        {/* ── Connectors tab ── */}
        {activeTab === 'connectors' && (
          <div className="settings-tab-content">
            {loadingConfiguration ? (
              <p>Loading configuration…</p>
            ) : connectorMetadata.length === 0 ? (
              <p>No connector types registered.</p>
            ) : (
              connectorMetadata.map((connector) => {
                const configKey = toConfigKey(connector.providerKey);
                const connections = configuration[configKey] ?? [];
                const helpKey = `settings.connectors.${connector.providerKey}`;

                return (
                  <section key={connector.providerKey} className="provider-config-block">
                    <div className="provider-config-header">
                      <div>
                        <h3>{connector.displayName} connections</h3>
                        <p>{connector.description}</p>
                      </div>
                      <button
                        className="add-button"
                        type="button"
                        onClick={() => addConnection(connector.providerKey, connector.configFields)}
                      >
                        Add connection
                      </button>
                    </div>

                    {helpContent[helpKey] ? (
                      <HelpPanel contextKey={helpKey} title={helpContent[helpKey].title}>
                        {helpContent[helpKey].body.map((line, i) => (
                          <p key={i}>{line}</p>
                        ))}
                      </HelpPanel>
                    ) : null}

                    <div className="connection-editor-list">
                      {connections.map((connection, index) => (
                        <article key={connection.id} className="connection-editor-card">
                          <div className="connection-editor-header">
                            <strong>{connection.name || `Connection ${index + 1}`}</strong>
                            <button
                              className="remove-button"
                              type="button"
                              onClick={() => removeConnection(configKey, index)}
                            >
                              Remove
                            </button>
                          </div>

                          <label className="toggle-row">
                            <input
                              type="checkbox"
                              checked={connection.enabled ?? true}
                              onChange={(e) => updateConnection(configKey, index, 'enabled', e.target.checked)}
                            />
                            <span>Enabled</span>
                          </label>

                          <div className="config-grid">
                            {connector.configFields.map((field) => {
                              const fieldError = validationErrors[configKey]?.[index]?.[field.key];

                              return field.inputKind === 'textarea' ? (
                                <label
                                  key={field.key}
                                  className={`config-field config-field-wide${fieldError ? ' config-field-error' : ''}`}
                                >
                                  <span>{field.label}</span>
                                  <textarea
                                    value={connection[field.key] ?? ''}
                                    onChange={(e) => updateConnection(configKey, index, field.key, e.target.value)}
                                    rows={4}
                                  />
                                  {fieldError ? <small>{fieldError}</small> : null}
                                </label>
                              ) : (
                                <label
                                  key={field.key}
                                  className={`config-field${fieldError ? ' config-field-error' : ''}`}
                                >
                                  <span>{field.label}</span>
                                  <input
                                    type={field.inputKind === 'password' ? 'password' : 'text'}
                                    value={connection[field.key] ?? ''}
                                    onChange={(e) => updateConnection(configKey, index, field.key, e.target.value)}
                                  />
                                  {fieldError ? <small>{fieldError}</small> : null}
                                </label>
                              );
                            })}
                          </div>
                        </article>
                      ))}
                    </div>
                  </section>
                );
              })
            )}

            <div className="configuration-actions">
              <button
                className="refresh-button"
                type="button"
                onClick={() => void handleSaveConfiguration()}
                disabled={savingConfiguration || loadingConfiguration}
              >
                {savingConfiguration ? 'Saving…' : 'Save connector configuration'}
              </button>
            </div>
          </div>
        )}

        {/* ── Account tab ── */}
        {activeTab === 'account' && (
          <div className="settings-tab-content">
            <div className="account-card">
              <div className="account-user-row">
                {user?.picture ? (
                  <img
                    className="nav-avatar"
                    src={user.picture}
                    alt={user.name || user.email || 'User'}
                    style={{ width: 56, height: 56 }}
                  />
                ) : (
                  <span
                    className="nav-avatar nav-avatar-fallback"
                    style={{ width: 56, height: 56, fontSize: '1.4rem' }}
                  >
                    {(user?.name || user?.email || 'U').slice(0, 1).toUpperCase()}
                  </span>
                )}
                <div>
                  <strong>{user?.name || user?.email || 'Signed in'}</strong>
                  <span>
                    {user?.email}
                    {user?.provider ? ` · ${formatProviderName(user.provider)}` : ''}
                  </span>
                </div>
              </div>
              <button
                className="signout-button"
                type="button"
                onClick={() => void handleSignOutClick()}
                disabled={signingOut}
                style={{ marginTop: 16 }}
              >
                {signingOut ? 'Signing out…' : 'Sign out'}
              </button>
            </div>
          </div>
        )}

        {/* ── Export tab ── */}
        {activeTab === 'export' && (
          <div className="settings-tab-content">
            <HelpPanel contextKey="settings.export" title={helpContent['settings.export'].title}>
              {helpContent['settings.export'].body.map((line, i) => (
                <p key={i}>{line}</p>
              ))}
            </HelpPanel>

            <div className="export-section">
              <article className="export-card">
                <h3>Export connector configuration</h3>
                <p>
                  Downloads your connector settings as JSON. PATs and tokens are replaced with masked placeholders
                  before download.
                </p>
                <button className="refresh-button" type="button" onClick={handleExportConfig}>
                  Download configuration
                </button>
              </article>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
