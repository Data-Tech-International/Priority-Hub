import { githubLoginUrl, microsoftLoginUrl } from './lib/auth.js';

function LoginPage({ error }) {
  return (
    <div className="app-shell">
      <main className="login-page">
        <section className="login-card">
          <p className="eyebrow">Priority Hub</p>
          <h1>Sign in to access your unified priority dashboard.</h1>
          <p className="login-copy">
            Use Microsoft or GitHub to unlock the workspace. Microsoft sign-in can also be used for Azure DevOps connections without storing a PAT.
          </p>

          {error ? (
            <section className="status-banner status-error">
              <strong>Authentication check failed.</strong>
              <span>{error}</span>
            </section>
          ) : null}

          <div className="login-actions">
            <a className="login-provider-button microsoft" href={microsoftLoginUrl}>
              Sign in with Microsoft
            </a>
            <a className="login-provider-button github" href={githubLoginUrl}>
              Sign in with GitHub
            </a>
          </div>
        </section>
      </main>
    </div>
  );
}

export default LoginPage;
