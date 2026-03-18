const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8787').replace(/\/$/, '');

export class UnauthorizedError extends Error {
  constructor(message = 'Authentication required.') {
    super(message);
    this.name = 'UnauthorizedError';
  }
}

const parseNdjsonStream = async (response, onEvent) => {
  if (!response.body) {
    throw new Error('Dashboard stream did not return a readable response body.');
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  while (true) {
    const { done, value } = await reader.read();
    if (done) {
      break;
    }

    buffer += decoder.decode(value, { stream: true });
    const lines = buffer.split('\n');
    buffer = lines.pop() ?? '';

    for (const line of lines) {
      const trimmed = line.trim();
      if (!trimmed) {
        continue;
      }

      onEvent(JSON.parse(trimmed));
    }
  }

  const trailing = buffer.trim();
  if (trailing) {
    onEvent(JSON.parse(trailing));
  }
};

const apiRequest = async (path, options) => {
  try {
    const response = await fetch(`${API_BASE_URL}${path}`, {
      headers: {
        'Content-Type': 'application/json',
        ...(options?.headers ?? {}),
      },
      credentials: 'include',
      ...options,
    });

    if (response.status === 401) {
      throw new UnauthorizedError();
    }

    if (!response.ok) {
      throw new Error(`${path} failed with ${response.status}`);
    }

    return response.status === 204 ? null : response.json();
  } catch (error) {
    if (error instanceof TypeError) {
      throw new Error('Priority Hub backend is unreachable. Start the ASP.NET Core API on http://localhost:8787 and try again.');
    }

    throw error;
  }
};

export const fetchDashboard = () => apiRequest('/api/dashboard');
export const fetchDashboardStream = async ({ signal, onEvent }) => {
  try {
    const response = await fetch(`${API_BASE_URL}/api/dashboard/stream`, {
      headers: {
        Accept: 'application/x-ndjson',
      },
      credentials: 'include',
      signal,
    });

    if (response.status === 401) {
      throw new UnauthorizedError();
    }

    if (!response.ok) {
      throw new Error(`/api/dashboard/stream failed with ${response.status}`);
    }

    await parseNdjsonStream(response, onEvent);
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') {
      throw error;
    }

    if (error instanceof TypeError) {
      throw new Error('Priority Hub backend is unreachable. Start the ASP.NET Core API on http://localhost:8787 and try again.');
    }

    throw error;
  }
};
export const fetchConfiguration = () => apiRequest('/api/config');
export const fetchConnectorMetadata = () => apiRequest('/api/connectors');
export const saveConfiguration = (configuration) => apiRequest('/api/config', {
  method: 'PUT',
  body: JSON.stringify(configuration),
});
export const saveItemOrder = (orderedItemIds) => apiRequest('/api/preferences/order', {
  method: 'PUT',
  body: JSON.stringify({ orderedItemIds }),
});