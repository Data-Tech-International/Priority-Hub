const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8787').replace(/\/$/, '');

export const microsoftLoginUrl = `${API_BASE_URL}/api/auth/login/microsoft`;
export const githubLoginUrl = `${API_BASE_URL}/api/auth/login/github`;

export const fetchCurrentUser = async () => {
  const response = await fetch(`${API_BASE_URL}/api/auth/me`, {
    credentials: 'include',
  });

  if (response.status === 401) {
    return null;
  }

  if (!response.ok) {
    throw new Error(`/api/auth/me failed with ${response.status}`);
  }

  return response.json();
};

export const signOut = async () => {
  const response = await fetch(`${API_BASE_URL}/api/auth/logout`, {
    method: 'POST',
    credentials: 'include',
  });

  if (response.status === 401) {
    return;
  }

  if (!response.ok) {
    throw new Error(`/api/auth/logout failed with ${response.status}`);
  }
};
