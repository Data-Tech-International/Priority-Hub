import { useEffect, useRef, useState } from 'react';

export function TagFilter({ availableTags, selectedTags, onTagsChange }) {
  const [open, setOpen] = useState(false);
  const containerRef = useRef(null);

  useEffect(() => {
    const handler = (e) => {
      if (containerRef.current && !containerRef.current.contains(e.target)) {
        setOpen(false);
      }
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  const toggle = (tag) => {
    onTagsChange(
      selectedTags.includes(tag)
        ? selectedTags.filter((t) => t !== tag)
        : [...selectedTags, tag],
    );
  };

  const label =
    selectedTags.length === 0
      ? 'All tags'
      : selectedTags.length === 1
        ? selectedTags[0]
        : `Tags (${selectedTags.length})`;

  return (
    <div className="tag-filter" ref={containerRef}>
      <span className="tag-filter-label">Tags</span>
      <button
        type="button"
        className={`tag-filter-toggle${open ? ' is-open' : ''}`}
        onClick={() => setOpen((prev) => !prev)}
      >
        {label}
      </button>
      {open && (
        <div className="tag-filter-dropdown">
          {selectedTags.length > 0 && (
            <button
              type="button"
              className="tag-filter-clear"
              onClick={() => {
                onTagsChange([]);
                setOpen(false);
              }}
            >
              Clear selection
            </button>
          )}
          {availableTags.length === 0 ? (
            <p className="tag-filter-empty">No tags in current data.</p>
          ) : (
            availableTags.map((tag) => (
              <label key={tag} className="tag-filter-option">
                <input
                  type="checkbox"
                  checked={selectedTags.includes(tag)}
                  onChange={() => toggle(tag)}
                />
                <span>{tag}</span>
              </label>
            ))
          )}
        </div>
      )}
    </div>
  );
}
