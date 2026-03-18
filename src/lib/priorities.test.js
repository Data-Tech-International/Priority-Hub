import { describe, expect, it } from 'vitest';
import { formatPriorityBand, formatProviderName, rankWorkItems } from './priorities';

describe('rankWorkItems', () => {
  it('places new items first, then respects manual order for existing items', () => {
    const boardConnections = [
      { id: 'b1', boardName: 'Board 1', projectName: 'Project A' },
      { id: 'b2', boardName: 'Board 2', projectName: 'Project B' },
    ];

    const items = [
      {
        id: 'existing-low',
        boardId: 'b1',
        title: 'Existing Low',
        impact: 2,
        urgency: 2,
        confidence: 5,
        ageDays: 1,
        blockerCount: 0,
        dueInDays: null,
        effort: 5,
        isNew: false,
      },
      {
        id: 'existing-high',
        boardId: 'b1',
        title: 'Existing High',
        impact: 8,
        urgency: 8,
        confidence: 9,
        ageDays: 2,
        blockerCount: 0,
        dueInDays: 1,
        effort: 3,
        isNew: false,
      },
      {
        id: 'new-item',
        boardId: 'b2',
        title: 'New Item',
        impact: 1,
        urgency: 1,
        confidence: 1,
        ageDays: 0,
        blockerCount: 0,
        dueInDays: null,
        effort: 1,
        isNew: true,
      },
    ];

    const orderedItemIds = ['existing-low', 'existing-high'];

    const ranked = rankWorkItems(items, boardConnections, orderedItemIds);

    expect(ranked.map((item) => item.id)).toEqual(['new-item', 'existing-low', 'existing-high']);
    expect(ranked[0].boardName).toBe('Board 2');
    expect(ranked[0].projectName).toBe('Project B');
  });

  it('scores urgent high-priority blocker above low-priority no-due-date item', () => {
    const boardConnections = [{ id: 'b1', boardName: 'Board', projectName: 'Project' }];

    const items = [
      {
        id: 'urgent',
        boardId: 'b1',
        title: 'Urgent',
        impact: 9,
        urgency: 10,
        confidence: 8,
        ageDays: 2,
        blockerCount: 1,
        dueInDays: 1,
        effort: 3,
        isNew: false,
      },
      {
        id: 'low',
        boardId: 'b1',
        title: 'Low',
        impact: 2,
        urgency: 2,
        confidence: 6,
        ageDays: 1,
        blockerCount: 0,
        dueInDays: null,
        effort: 5,
        isNew: false,
      },
    ];

    const ranked = rankWorkItems(items, boardConnections);
    const urgent = ranked.find((item) => item.id === 'urgent');
    const low = ranked.find((item) => item.id === 'low');

    expect(urgent.score).toBeGreaterThan(low.score);
  });
});

describe('provider and band formatting', () => {
  it('formats known providers', () => {
    expect(formatProviderName('azure-devops')).toBe('Azure DevOps');
    expect(formatProviderName('microsoft-tasks')).toBe('Microsoft Tasks');
    expect(formatProviderName('outlook-flagged-mail')).toBe('Outlook Flagged Mail');
    expect(formatProviderName('unknown-provider')).toBe('unknown-provider');
  });

  it('formats priority bands', () => {
    expect(formatPriorityBand('critical')).toBe('Critical now');
    expect(formatPriorityBand('focus')).toBe('Focus next');
    expect(formatPriorityBand('maintain')).toBe('Maintain');
    expect(formatPriorityBand('other')).toBe('other');
  });
});
