const bandThresholds = [
  { minScore: 82, band: 'critical' },
  { minScore: 60, band: 'focus' },
  { minScore: 0, band: 'maintain' },
];

const clamp = (value, min, max) => Math.min(max, Math.max(min, value));

const dueDateWeight = (dueInDays) => {
  if (dueInDays === null || dueInDays === undefined) {
    return 4;
  }

  if (dueInDays <= 1) return 14;
  if (dueInDays <= 3) return 10;
  if (dueInDays <= 7) return 6;
  return 2;
};

export const rankWorkItems = (items, boardConnections, orderedItemIds = []) => {
  const orderIndex = new Map(orderedItemIds.map((itemId, index) => [itemId, index]));

  const enrichedItems = items
    .map((item) => {
      const board = boardConnections.find((connection) => connection.id === item.boardId);

      if (!board) {
        return null;
      }

      const score = clamp(
        item.impact * 4 +
          item.urgency * 3 +
          item.confidence * 1.5 +
          Math.min(item.ageDays, 10) +
          item.blockerCount * 4 +
          dueDateWeight(item.dueInDays) -
          item.effort * 2,
        0,
        100,
      );

      return {
        ...item,
        score,
        band: bandThresholds.find((threshold) => score >= threshold.minScore)?.band ?? 'maintain',
        boardName: board.boardName,
        projectName: board.projectName,
        orderIndex: orderIndex.has(item.id) ? orderIndex.get(item.id) : Number.MAX_SAFE_INTEGER,
      };
    })
    .filter(Boolean);

  const newItems = enrichedItems
    .filter((item) => item.isNew)
    .sort((left, right) => right.score - left.score);

  const existingItems = enrichedItems
    .filter((item) => !item.isNew)
    .sort((left, right) => {
      if (left.orderIndex !== right.orderIndex) {
        return left.orderIndex - right.orderIndex;
      }

      return right.score - left.score;
    });

  return [...newItems, ...existingItems];
};

export const formatProviderName = (provider) => {
  switch (provider) {
    case 'microsoft':
      return 'Microsoft';
    case 'github':
      return 'GitHub';
    case 'azure-devops':
      return 'Azure DevOps';
    case 'jira':
      return 'Jira';
    case 'microsoft-tasks':
      return 'Microsoft Tasks';
    case 'outlook-flagged-mail':
      return 'Outlook Flagged Mail';
    case 'trello':
      return 'Trello';
    default:
      return provider;
  }
};

export const formatPriorityBand = (band) => {
  switch (band) {
    case 'critical':
      return 'Critical now';
    case 'focus':
      return 'Focus next';
    case 'maintain':
      return 'Maintain';
    default:
      return band;
  }
};