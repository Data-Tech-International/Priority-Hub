import { useState } from 'react';

export function HelpPanel({ contextKey, title, children }) {
  const [open, setOpen] = useState(() => {
    try {
      return localStorage.getItem(`phub:help:${contextKey}`) === 'open';
    } catch {
      return false;
    }
  });

  const toggle = () => {
    const next = !open;
    setOpen(next);
    try {
      localStorage.setItem(`phub:help:${contextKey}`, next ? 'open' : 'closed');
    } catch {
      // ignore storage errors
    }
  };

  return (
    <div className="help-panel">
      <button type="button" className="help-toggle" onClick={toggle} aria-expanded={open}>
        <span className="help-icon">?</span>
        {title}
        <span className="help-chevron">{open ? '▲' : '▼'}</span>
      </button>
      {open && <div className="help-content">{children}</div>}
    </div>
  );
}
