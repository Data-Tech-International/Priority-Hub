import { beforeEach, describe, expect, it } from 'vitest';
import { clearDashboardCache, readDashboardCache, writeDashboardCache } from './dashboardCache';

describe('dashboardCache', () => {
  const userSub = 'user-123';
  const dashboard = {
    boardConnections: [{ id: 'b1' }],
    workItems: [{ id: 'w1' }],
    issues: [],
    preferences: { orderedItemIds: ['w1'] },
    generatedAt: '2026-03-17T00:00:00Z',
  };
  const progress = {
    totalConnections: 2,
    completedConnections: 1,
    activeProvider: 'jira',
    activeConnectionId: 'j1',
    activeConnectionName: 'Jira',
    message: 'Loading',
    isComplete: false,
  };

  beforeEach(() => {
    window.localStorage.clear();
  });

  it('writes and reads cached dashboard snapshots per user', () => {
    writeDashboardCache(userSub, dashboard, progress);

    const cached = readDashboardCache(userSub);

    expect(cached.dashboard).toEqual(dashboard);
    expect(cached.progress).toEqual(progress);
    expect(cached.cachedAt).toBeTypeOf('string');
  });

  it('returns null when cache is missing or malformed', () => {
    expect(readDashboardCache(userSub)).toBeNull();

    window.localStorage.setItem('priority-hub.dashboard.user-123', '{bad json');
    expect(readDashboardCache(userSub)).toBeNull();
  });

  it('clears cache for one user without affecting another user', () => {
    writeDashboardCache('user-123', dashboard, progress);
    writeDashboardCache('user-456', { ...dashboard, workItems: [] }, progress);

    clearDashboardCache('user-123');

    expect(readDashboardCache('user-123')).toBeNull();
    expect(readDashboardCache('user-456')?.dashboard.workItems).toEqual([]);
  });
});
