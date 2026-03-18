import { Navigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext.jsx';

export function ProtectedRoute({ children }) {
  const { user, loadingAuth } = useAuth();

  if (loadingAuth) {
    return (
      <div className="app-shell">
        <main className="login-page">
          <section className="login-card">
            <p className="eyebrow">Priority Hub</p>
            <h1>Checking your session.</h1>
            <p className="login-copy">
              Validating the current sign-in before loading your dashboard.
            </p>
          </section>
        </main>
      </div>
    );
  }

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  return children;
}
