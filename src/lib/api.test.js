import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  UnauthorizedError,
  fetchConfiguration,
  fetchConnectorMetadata,
  fetchDashboard,
  fetchDashboardStream,
  saveConfiguration,
  saveItemOrder,
} from './api';

const createJsonResponse = ({ ok = true, status = 200, json }) => ({
  ok,
  status,
  json: vi.fn().mockResolvedValue(json),
});

const createStreamResponse = (chunks, status = 200) => {
  let index = 0;

  return {
    ok: status >= 200 && status < 300,
    status,
    body: {
      getReader: () => ({
        read: vi.fn().mockImplementation(async () => {
          if (index >= chunks.length) {
            return { done: true, value: undefined };
          }

          const value = new TextEncoder().encode(chunks[index]);
          index += 1;
          return { done: false, value };
        }),
      }),
    },
  };
};

describe('api helpers', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn());
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it('fetchDashboard returns parsed dashboard data', async () => {
    const payload = { workItems: [{ id: '1' }] };
    fetch.mockResolvedValue(createJsonResponse({ json: payload }));

    await expect(fetchDashboard()).resolves.toEqual(payload);
    expect(fetch).toHaveBeenCalledWith(
      'http://localhost:8787/api/dashboard',
      expect.objectContaining({ credentials: 'include' }),
    );
  });

  it('fetchConfiguration and fetchConnectorMetadata call the expected endpoints', async () => {
    fetch
      .mockResolvedValueOnce(createJsonResponse({ json: { theme: 'light' } }))
      .mockResolvedValueOnce(createJsonResponse({ json: [{ key: 'jira' }] }));

    await expect(fetchConfiguration()).resolves.toEqual({ theme: 'light' });
    await expect(fetchConnectorMetadata()).resolves.toEqual([{ key: 'jira' }]);
  });

  it('saveConfiguration and saveItemOrder send JSON payloads', async () => {
    fetch
      .mockResolvedValueOnce(createJsonResponse({ json: { ok: true } }))
      .mockResolvedValueOnce(createJsonResponse({ json: { saved: true } }));

    await saveConfiguration({ jira: [] });
    await saveItemOrder(['a', 'b']);

    expect(fetch).toHaveBeenNthCalledWith(
      1,
      'http://localhost:8787/api/config',
      expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify({ jira: [] }),
      }),
    );
    expect(fetch).toHaveBeenNthCalledWith(
      2,
      'http://localhost:8787/api/preferences/order',
      expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify({ orderedItemIds: ['a', 'b'] }),
      }),
    );
  });

  it('throws UnauthorizedError on 401 responses', async () => {
    fetch.mockResolvedValue(createJsonResponse({ ok: false, status: 401 }));

    await expect(fetchDashboard()).rejects.toBeInstanceOf(UnauthorizedError);
  });

  it('wraps network errors with a backend-unreachable message', async () => {
    fetch.mockRejectedValue(new TypeError('network down'));

    await expect(fetchDashboard()).rejects.toThrow('Priority Hub backend is unreachable');
  });

  it('parses streamed NDJSON dashboard events', async () => {
    const events = [];
    fetch.mockResolvedValue(
      createStreamResponse([
        '{"phase":"loading"}\n{"phase":"partial",',
        '"count":2}\n',
      ]),
    );

    await fetchDashboardStream({
      signal: undefined,
      onEvent: (event) => events.push(event),
    });

    expect(events).toEqual([
      { phase: 'loading' },
      { phase: 'partial', count: 2 },
    ]);
  });

  it('rethrows abort errors for dashboard stream requests', async () => {
    const abortError = new DOMException('Request aborted', 'AbortError');
    fetch.mockRejectedValue(abortError);

    await expect(
      fetchDashboardStream({ signal: undefined, onEvent: vi.fn() }),
    ).rejects.toBe(abortError);
  });
});