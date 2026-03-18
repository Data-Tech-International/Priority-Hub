import { useState } from 'react';
import { NavLink } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext.jsx';
import { formatProviderName } from '../lib/priorities.js';

export function NavBar() {
  const { user, handleSignOut } = useAuth();
  const [signingOut, setSigningOut] = useState(false);

  if (!user) return null;

  const handleSignOutClick = async () => {
    setSigningOut(true);
    try {
      await handleSignOut();
    } finally {
      setSigningOut(false);
    }
  };

  return (
    <header className="nav-bar">
      <div className="nav-bar-inner">
        <nav className="nav-links">
          <NavLink to="/" end className={({ isActive }) => `nav-link${isActive ? ' is-active' : ''}`}>
            Dashboard
          </NavLink>
          <NavLink to="/settings" className={({ isActive }) => `nav-link${isActive ? ' is-active' : ''}`}>
            Settings
          </NavLink>
        </nav>
        <div className="nav-user">
          {user.picture ? (
            <img className="nav-avatar" src={user.picture} alt={user.name || user.email || 'User'} />
          ) : (
            <span className="nav-avatar nav-avatar-fallback">
              {(user.name || user.email || 'U').slice(0, 1).toUpperCase()}
            </span>
          )}
          <div className="nav-user-info">
            <strong>{user.name || user.email || 'Signed in'}</strong>
            <span>{user.email || `${formatProviderName(user.provider)} account`}</span>
          </div>
          <button
            className="signout-button"
            type="button"
            onClick={() => void handleSignOutClick()}
            disabled={signingOut}
          >
            {signingOut ? 'Signing out…' : 'Sign out'}
          </button>
        </div>
      </div>
    </header>
  );
}
