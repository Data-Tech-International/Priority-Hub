import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  fetchCurrentUser,
  githubLoginUrl,
  microsoftLoginUrl,
  signOut,
} from './auth';

describe('auth helpers', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn());
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it('exports provider login URLs based on the local backend URL', () => {
    expect(microsoftLoginUrl).toBe('http://localhost:8787/api/auth/login/microsoft');
    expect(githubLoginUrl).toBe('http://localhost:8787/api/auth/login/github');
  });

  it('returns the current user payload when authenticated', async () => {
    const user = { id: 'user-1', name: 'Ivan' };
    fetch.mockResolvedValue({
      ok: true,
      status: 200,
      json: vi.fn().mockResolvedValue(user),
    });

    await expect(fetchCurrentUser()).resolves.toEqual(user);
  });

  it('returns null when the current user endpoint is unauthorized', async () => {
    fetch.mockResolvedValue({ ok: false, status: 401 });

    await expect(fetchCurrentUser()).resolves.toBeNull();
  });

  it('throws for unexpected current user failures', async () => {
    fetch.mockResolvedValue({ ok: false, status: 500 });

    await expect(fetchCurrentUser()).rejects.toThrow('/api/auth/me failed with 500');
  });

  it('posts to logout and resolves quietly for unauthorized users', async () => {
    fetch
      .mockResolvedValueOnce({ ok: true, status: 200 })
      .mockResolvedValueOnce({ ok: false, status: 401 });

    await expect(signOut()).resolves.toBeUndefined();
    await expect(signOut()).resolves.toBeUndefined();
    expect(fetch).toHaveBeenCalledWith(
      'http://localhost:8787/api/auth/logout',
      expect.objectContaining({ method: 'POST', credentials: 'include' }),
    );
  });

  it('throws for unexpected logout failures', async () => {
    fetch.mockResolvedValue({ ok: false, status: 500 });

    await expect(signOut()).rejects.toThrow('/api/auth/logout failed with 500');
  });
});