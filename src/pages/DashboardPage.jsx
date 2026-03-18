import { useEffect, useMemo, useRef, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext.jsx';
import { HelpPanel } from '../components/HelpPanel.jsx';
import { TagFilter } from '../components/TagFilter.jsx';
import { UnauthorizedError, fetchDashboardStream, saveItemOrder } from '../lib/api.js';
import { readDashboardCache, writeDashboardCache } from '../lib/dashboardCache.js';
import { formatPriorityBand, formatProviderName, rankWorkItems } from '../lib/priorities.js';
import { helpContent } from '../data/helpContent.js';

const providerOptions = ['all', 'azure-devops', 'github', 'jira', 'microsoft-tasks', 'outlook-flagged-mail', 'trello'];
const statusOptions = ['all', 'blocked', 'in-progress', 'review', 'planned', 'done'];
const priorityOptions = ['all', 'critical', 'focus', 'maintain'];

const emptyDashboard = {
  boardConnections: [],
  workItems: [],
  issues: [],
  preferences: { orderedItemIds: [] },
  generatedAt: null,
};

const emptyDashboardProgress = {
  totalConnections: 0,
  completedConnections: 0,
  activeProvider: '',
  activeConnectionId: '',
  activeConnectionName: '',
  message: '',
  isComplete: true,
};

const fetchedDashboardUsers = new Set();

const reorderVisibleItems = (currentVisibleIds, existingOrderedIds, draggedId, targetId) => {
  const workingIds = [...currentVisibleIds];
  const draggedIndex = workingIds.indexOf(draggedId);
  const targetIndex = workingIds.indexOf(targetId);

  if (draggedIndex === -1 || targetIndex === -1 || draggedIndex === targetIndex) {
    return existingOrderedIds;
  }

  const [draggedItemId] = workingIds.splice(draggedIndex, 1);
  workingIds.splice(targetIndex, 0, draggedItemId);

  const hiddenIds = existingOrderedIds.filter((id) => !currentVisibleIds.includes(id) && id !== draggedId);
  return [...workingIds, ...hiddenIds];
};

export default function DashboardPage() {
  const { user, handleUnauthorized } = useAuth();
  const location = useLocation();

  const [dashboard, setDashboard] = useState(emptyDashboard);
  const [orderedItemIds, setOrderedItemIds] = useState([]);
  const [dashboardProgress, setDashboardProgress] = useState(emptyDashboardProgress);
  const [loadingDashboard, setLoadingDashboard] = useState(true);
  const [error, setError] = useState(null);
  const [statusMessage, setStatusMessage] = useState(location.state?.configMessage ?? null);
  const [selectedProvider, setSelectedProvider] = useState('all');
  const [selectedStatus, setSelectedStatus] = useState('all');
  const [selectedBand, setSelectedBand] = useState('all');
  const [selectedTags, setSelectedTags] = useState([]);
  const [search, setSearch] = useState('');
  const [draggedItemId, setDraggedItemId] = useState(null);
  const [savingOrder, setSavingOrder] = useState(false);
  const [showConnectors, setShowConnectors] = useState(false);
  const dashboardAbortControllerRef = useRef(null);

  const resetDashboardState = () => {
    dashboardAbortControllerRef.current?.abort();
    dashboardAbortControllerRef.current = null;
    setDashboard(emptyDashboard);
    setOrderedItemIds([]);
    setDashboardProgress(emptyDashboardProgress);
    setLoadingDashboard(false);
  };

  const loadDashboard = async ({ force = false } = {}) => {
    dashboardAbortControllerRef.current?.abort();
    const abortController = new AbortController();
    dashboardAbortControllerRef.current = abortController;

    setLoadingDashboard(true);
    setError(null);
    setDashboardProgress({ ...emptyDashboardProgress, isComplete: false, message: 'Starting connector refresh.' });

    try {
      await fetchDashboardStream({
        signal: abortController.signal,
        onEvent: (event) => {
          if (event?.type !== 'snapshot') return;
          const nextDashboard = event.dashboard ?? emptyDashboard;
          if (user?.sub) {
            writeDashboardCache(user.sub, nextDashboard, event.progress ?? emptyDashboardProgress);
          }
          setDashboard(nextDashboard);
          setOrderedItemIds(nextDashboard.preferences?.orderedItemIds ?? []);
          setDashboardProgress(event.progress ?? emptyDashboardProgress);
        },
      });

      if (user?.sub) {
        fetchedDashboardUsers.add(user.sub);
      }
    } catch (loadError) {
      if (loadError instanceof DOMException && loadError.name === 'AbortError') return;

      if (loadError instanceof UnauthorizedError) {
        handleUnauthorized();
        return;
      }

      setError(loadError instanceof Error ? loadError.message : 'Unable to load dashboard data.');
    } finally {
      if (dashboardAbortControllerRef.current === abortController) {
        dashboardAbortControllerRef.current = null;
        setLoadingDashboard(false);
      }
    }
  };

  useEffect(() => {
    if (!user?.sub) {
      resetDashboardState();
      return () => { dashboardAbortControllerRef.current?.abort(); };
    }

    const cached = readDashboardCache(user.sub);
    if (cached) {
      setDashboard(cached.dashboard);
      setOrderedItemIds(cached.dashboard.preferences?.orderedItemIds ?? []);
      setDashboardProgress(cached.progress ?? emptyDashboardProgress);
      setLoadingDashboard(false);
    }

    if (!fetchedDashboardUsers.has(user.sub) || !cached) {
      void loadDashboard({ force: true });
    }

    return () => { dashboardAbortControllerRef.current?.abort(); };
  }, [user?.sub]);

  const boardConnections = dashboard?.boardConnections ?? [];
  const issues = dashboard?.issues ?? [];

  const rankedItems = useMemo(
    () => rankWorkItems(dashboard?.workItems ?? [], boardConnections, orderedItemIds),
    [dashboard, boardConnections, orderedItemIds],
  );

  const availableTags = useMemo(
    () => [...new Set((dashboard?.workItems ?? []).flatMap((item) => item.tags ?? []))].sort(),
    [dashboard?.workItems],
  );

  const progressPercent = dashboardProgress.totalConnections > 0
    ? Math.round((dashboardProgress.completedConnections / dashboardProgress.totalConnections) * 100)
    : loadingDashboard ? 8 : 100;

  const filteredItems = useMemo(() => {
    const query = search.trim().toLowerCase();

    return rankedItems.filter((item) => {
      const matchesProvider = selectedProvider === 'all' || item.provider === selectedProvider;
      const matchesStatus = selectedStatus === 'all' || item.status === selectedStatus;
      const matchesBand = selectedBand === 'all' || item.band === selectedBand;
      const matchesTags = selectedTags.length === 0 || selectedTags.some((tag) => item.tags.includes(tag));
      const matchesQuery =
        query.length === 0 ||
        item.title.toLowerCase().includes(query) ||
        item.id.toLowerCase().includes(query) ||
        item.tags.some((tag) => tag.toLowerCase().includes(query));

      return matchesProvider && matchesStatus && matchesBand && matchesTags && matchesQuery;
    });
  }, [rankedItems, search, selectedBand, selectedProvider, selectedStatus, selectedTags]);

  const summary = useMemo(() => {
    const criticalCount = rankedItems.filter((i) => i.band === 'critical').length;
    const blockedCount = rankedItems.filter((i) => i.status === 'blocked').length;
    const averageScore = rankedItems.length > 0
      ? Math.round(rankedItems.reduce((sum, i) => sum + i.score, 0) / rankedItems.length)
      : 0;
    const newCount = rankedItems.filter((i) => i.isNew).length;
    return {
      visibleItems: filteredItems.length,
      totalItems: rankedItems.length,
      criticalCount,
      blockedCount,
      averageScore,
      newCount,
    };
  }, [filteredItems.length, rankedItems]);

  const handleDropItem = async (targetId) => {
    if (!draggedItemId || draggedItemId === targetId) {
      setDraggedItemId(null);
      return;
    }

    const currentVisibleIds = rankedItems.map((item) => item.id);
    const nextOrderedIds = reorderVisibleItems(currentVisibleIds, orderedItemIds, draggedItemId, targetId);
    setOrderedItemIds(nextOrderedIds);
    setSavingOrder(true);
    setStatusMessage(null);

    try {
      const savedPreferences = await saveItemOrder(nextOrderedIds);
      setOrderedItemIds(savedPreferences.orderedItemIds ?? nextOrderedIds);
      setStatusMessage('Queue order saved. New items stay highlighted until placed in your manual order.');
    } catch (saveError) {
      if (saveError instanceof UnauthorizedError) {
        handleUnauthorized();
        return;
      }
      setStatusMessage(saveError instanceof Error ? saveError.message : 'Could not save queue order.');
    } finally {
      setSavingOrder(false);
      setDraggedItemId(null);
    }
  };

  return (
    <div className="app-shell">
      <main className="dashboard">
        <section className="hero-panel">
          <div className="hero-copy">
            <p className="eyebrow">Priority Hub</p>
            <h1>Manually ordered priorities across Azure DevOps, GitHub, Jira, and Trello.</h1>
            <p className="hero-text">
              Add only the fields each connector truly needs, validate them in the browser, then drag cards into your
              own persistent cross-source order.
            </p>
            <div className="hero-actions">
              <button className="refresh-button" onClick={() => void loadDashboard({ force: true })} type="button">
                {loadingDashboard ? 'Refreshing…' : 'Refresh live data'}
              </button>
              <span className="generated-at">
                {dashboard.generatedAt
                  ? `Last aggregated ${new Date(dashboard.generatedAt).toLocaleString()}`
                  : 'Waiting for server data'}
              </span>
            </div>

            <div className="fetch-progress-panel" aria-live="polite">
              <div className="fetch-progress-topline">
                <strong>{loadingDashboard ? 'Fetching live connector data' : 'Live connector refresh complete'}</strong>
                <span>
                  {dashboardProgress.totalConnections > 0
                    ? `${dashboardProgress.completedConnections}/${dashboardProgress.totalConnections} sources`
                    : 'No enabled sources'}
                </span>
              </div>
              <div
                className="fetch-progress-track"
                role="progressbar"
                aria-valuemin={0}
                aria-valuemax={100}
                aria-valuenow={progressPercent}
              >
                <span className="fetch-progress-fill" style={{ width: `${progressPercent}%` }} />
              </div>
              <p className="fetch-progress-text">
                {dashboardProgress.message ||
                  (loadingDashboard ? 'Waiting for connector responses.' : 'All configured connectors are idle.')}
              </p>
            </div>
          </div>

          <div className="hero-metrics">
            <article className="metric-card accent-sand">
              <span className="metric-label">Visible items</span>
              <strong>
                {summary.visibleItems}/{summary.totalItems}
              </strong>
              <p>Current filtered items over the total fetched queue across all enabled sources.</p>
            </article>
            <article className="metric-card accent-terracotta">
              <span className="metric-label">Critical now</span>
              <strong>{summary.criticalCount}</strong>
              <p>Known items follow your manual order. New items stay loud until you place them.</p>
            </article>
            <article className="metric-card accent-slate">
              <span className="metric-label">Blocked items</span>
              <strong>{summary.blockedCount}</strong>
              <p>Cross-provider blockers remain visible in the same queue.</p>
            </article>
            <article className="metric-card accent-mint">
              <span className="metric-label">New items</span>
              <strong>{summary.newCount}</strong>
              <p>Newly retrieved items are highlighted and floated to the top until ordered.</p>
            </article>
          </div>
        </section>

        {error ? (
          <section className="status-banner status-error">
            <strong>Dashboard fetch failed.</strong>
            <span>{error}</span>
          </section>
        ) : null}

        {statusMessage ? (
          <section className="status-banner status-info">
            <strong>Status</strong>
            <span>{statusMessage}</span>
          </section>
        ) : null}

        {issues.length > 0 ? (
          <section className="status-banner status-warning">
            <strong>Some connectors need attention.</strong>
            <span>
              {issues
                .map((issue) => `${formatProviderName(issue.provider)} (${issue.connectionId}): ${issue.message}`)
                .join(' | ')}
            </span>
          </section>
        ) : null}

        <section className="content-grid">
          <section className="panel filters-panel">
              <div className="panel-header">
                <div>
                  <h2>Unified work queue</h2>
                  <p>Drag cards to keep one manual order across all connected sources.</p>
                </div>
                <span className="queue-hint">{savingOrder ? 'Saving order…' : 'Drag and drop to reorder'}</span>
              </div>

              <HelpPanel contextKey="dashboard.overview" title={helpContent['dashboard.overview'].title}>
                {helpContent['dashboard.overview'].body.map((line, i) => (
                  <p key={i}>{line}</p>
                ))}
              </HelpPanel>

              {loadingDashboard ? (
                <div className="queue-streaming-banner">
                  <strong>Items appear as each source finishes.</strong>
                  <span>
                    {dashboardProgress.totalConnections > 0
                      ? `${dashboardProgress.completedConnections} of ${dashboardProgress.totalConnections} connector fetches completed.`
                      : 'Connector refresh has started.'}
                  </span>
                </div>
              ) : null}

              <HelpPanel contextKey="dashboard.filters" title={helpContent['dashboard.filters'].title}>
                {helpContent['dashboard.filters'].body.map((line, i) => (
                  <p key={i}>{line}</p>
                ))}
              </HelpPanel>

              <div className="filters-row">
                <label>
                  <span>Search</span>
                  <input
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    placeholder="Search title, ID, or tag"
                  />
                </label>
                <label>
                  <span>Provider</span>
                  <select value={selectedProvider} onChange={(e) => setSelectedProvider(e.target.value)}>
                    {providerOptions.map((option) => (
                      <option key={option} value={option}>
                        {option === 'all' ? 'All providers' : formatProviderName(option)}
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  <span>Status</span>
                  <select value={selectedStatus} onChange={(e) => setSelectedStatus(e.target.value)}>
                    {statusOptions.map((option) => (
                      <option key={option} value={option}>
                        {option === 'all' ? 'All statuses' : option}
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  <span>Priority band</span>
                  <select value={selectedBand} onChange={(e) => setSelectedBand(e.target.value)}>
                    {priorityOptions.map((option) => (
                      <option key={option} value={option}>
                        {option === 'all' ? 'All bands' : formatPriorityBand(option)}
                      </option>
                    ))}
                  </select>
                </label>
                <TagFilter
                  availableTags={availableTags}
                  selectedTags={selectedTags}
                  onTagsChange={setSelectedTags}
                />
              </div>

              <section className="compact-connectors">
                <button
                  type="button"
                  className="compact-connectors-toggle"
                  onClick={() => setShowConnectors((current) => !current)}
                >
                  <span>
                    Live connectors: {boardConnections.length > 0
                      ? `${summary.connectedBoards}/${boardConnections.length} connected`
                      : 'none configured'}
                  </span>
                  <span>{showConnectors ? 'Hide' : 'Show'}</span>
                </button>

                {showConnectors ? (
                  <div className="connector-list connector-list-compact">
                    {boardConnections.map((board) => (
                      <article key={board.id} className="connector-card">
                        <div className="connector-topline">
                          <strong>{board.boardName}</strong>
                          <span className={`sync-pill sync-${board.syncStatus}`}>{board.syncStatus}</span>
                        </div>
                        <p>
                          {formatProviderName(board.provider)} · {board.projectName}
                        </p>
                        <p>{board.workspaceName}</p>
                      </article>
                    ))}

                    {!loadingDashboard && boardConnections.length === 0 ? (
                      <article className="connector-card">
                        <strong>No live connector instances yet.</strong>
                        <p>
                          Open Settings to add Azure DevOps projects, Jira queries, or Trello boards.
                        </p>
                      </article>
                    ) : null}
                  </div>
                ) : null}
              </section>

              <div className="item-list">
                {loadingDashboard ? (
                  <div className="empty-state">
                    <h3>Loading live board data.</h3>
                    <p>The queue will populate incrementally as each connector finishes streaming results.</p>
                  </div>
                ) : null}

                {filteredItems.map((item) => (
                  <article
                    key={item.id}
                    className={`work-item-card${item.isNew ? ' is-new-item' : ''}${draggedItemId === item.id ? ' is-dragging' : ''}`}
                    draggable={!savingOrder}
                    onDragStart={() => setDraggedItemId(item.id)}
                    onDragEnd={() => setDraggedItemId(null)}
                    onDragOver={(e) => e.preventDefault()}
                    onDrop={() => void handleDropItem(item.id)}
                  >
                    <div className="work-item-heading">
                      <div className="work-item-title">
                        <h3>{item.title}</h3>
                        {item.isNew ? <span className="new-pill">New item</span> : null}
                      </div>
                      {item.sourceUrl ? (
                        <a
                          href={item.sourceUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="item-source-link"
                          draggable={false}
                          onDragStart={(event) => event.preventDefault()}
                        >
                          Open in source
                        </a>
                      ) : null}
                    </div>

                    <div className="tag-row">
                      {item.tags.length === 0 ? <span className="tag-chip tag-chip-empty">No tags</span> : null}
                      {item.tags.map((tag) => (
                        <span key={tag} className="tag-chip">
                          {tag}
                        </span>
                      ))}
                    </div>
                  </article>
                ))}

                {!loadingDashboard && filteredItems.length === 0 ? (
                  <div className="empty-state">
                    <h3>No work items match the current filters.</h3>
                    <p>
                      Save provider connections in Settings and refresh the dashboard to start aggregating real data.
                    </p>
                  </div>
                ) : null}
              </div>
            </section>
        </section>
      </main>
    </div>
  );
}
