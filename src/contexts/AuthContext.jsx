import { createContext, useCallback, useContext, useEffect, useState } from 'react';
import { fetchCurrentUser, signOut } from '../lib/auth.js';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [loadingAuth, setLoadingAuth] = useState(true);
  const [authError, setAuthError] = useState(null);

  useEffect(() => {
    fetchCurrentUser()
      .then((nextUser) => {
        setUser(nextUser);
        setAuthError(null);
      })
      .catch((err) => {
        setUser(null);
        setAuthError(err instanceof Error ? err.message : 'Unable to verify your session.');
      })
      .finally(() => {
        setLoadingAuth(false);
      });
  }, []);

  const handleUnauthorized = useCallback(() => {
    setUser(null);
    setAuthError(null);
  }, []);

  const handleSignOut = useCallback(async () => {
    await signOut();
    setUser(null);
    setAuthError(null);
  }, []);

  return (
    <AuthContext.Provider value={{ user, loadingAuth, authError, handleUnauthorized, handleSignOut }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => useContext(AuthContext);
