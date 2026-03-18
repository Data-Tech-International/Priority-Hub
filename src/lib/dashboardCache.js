const dashboardCacheKey = (userSub) => `priority-hub.dashboard.${userSub}`;

export const readDashboardCache = (userSub) => {
  if (!userSub) {
    return null;
  }

  try {
    const raw = window.localStorage.getItem(dashboardCacheKey(userSub));
    if (!raw) {
      return null;
    }

    const parsed = JSON.parse(raw);
    if (!parsed?.dashboard) {
      return null;
    }

    return parsed;
  } catch {
    return null;
  }
};

export const writeDashboardCache = (userSub, dashboard, progress) => {
  if (!userSub) {
    return;
  }

  window.localStorage.setItem(
    dashboardCacheKey(userSub),
    JSON.stringify({
      dashboard,
      progress,
      cachedAt: new Date().toISOString(),
    }),
  );
};

export const clearDashboardCache = (userSub) => {
  if (!userSub) {
    return;
  }

  window.localStorage.removeItem(dashboardCacheKey(userSub));
};
