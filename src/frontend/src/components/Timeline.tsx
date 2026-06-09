import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  listEntries,
  deleteEntry,
  isEntryImmutable,
  formatEntryDate,
  getConfidentialityColor,
  Entry,
  PagedResult,
} from '../services/entriesClient';
import { MindmapView } from './MindmapView';
import { SentimentChart } from './SentimentChart';
import { downloadExport, ExportFormat } from '../services/exportClient';
import { asApiError, getErrorMessage } from '../utils/errors';
import '../styles/entry.css';

type ViewMode = 'list' | 'mindmap' | 'sentiment';

const VIEW_MODES: { mode: ViewMode; label: string; title: string }[] = [
  { mode: 'list', label: '📋 List', title: 'List View' },
  { mode: 'mindmap', label: '🧠 Mindmap', title: 'Mindmap View' },
  { mode: 'sentiment', label: '📈 Sentiment', title: 'Sentiment Trends' },
];

const ViewModeSelector: React.FC<{
  viewMode: ViewMode;
  onChange: (mode: ViewMode) => void;
}> = ({ viewMode, onChange }) => (
  <div className="view-mode-selector">
    {VIEW_MODES.map(({ mode, label, title }) => (
      <button
        key={mode}
        className={`view-btn ${viewMode === mode ? 'active' : ''}`}
        onClick={() => onChange(mode)}
        title={title}
      >
        {label}
      </button>
    ))}
  </div>
);

/**
 * Timeline Component
 * Displays a list of journal entries with pagination, confidentiality badges,
 * immutability indicators, and action buttons for edit/delete
 */

export const Timeline: React.FC = () => {
  const navigate = useNavigate();
  const [entries, setEntries] = useState<Entry[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [deleting, setDeleting] = useState<string | null>(null);
  const [viewMode, setViewMode] = useState<ViewMode>('list');
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [exporting, setExporting] = useState<ExportFormat | null>(null);

  const handleExport = async (format: ExportFormat) => {
    setExporting(format);
    setError(null);
    try {
      await downloadExport(format);
    } catch (err) {
      setError(getErrorMessage(err, 'Export failed'));
    } finally {
      setExporting(null);
    }
  };

  // Debounce the search box so we don't fetch on every keystroke
  useEffect(() => {
    const handle = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(handle);
  }, [search]);

  // A new search resets to the first page
  useEffect(() => {
    setPage(1);
  }, [debouncedSearch]);

  // Load entries on page or search change
  useEffect(() => {
    const fetchEntries = async () => {
      setLoading(true);
      setError(null);

      try {
        const result: PagedResult<Entry> = await listEntries(page, 20, { q: debouncedSearch });

        setEntries(result.items);
        setTotalPages(result.totalPages);
      } catch (err) {
        // Check if it's a 401 - likely means token is stale
        if (asApiError(err).response?.status === 401) {
          console.error('[Timeline] Got 401 - token is likely stale. Clearing storage and redirecting to login...');
          localStorage.removeItem('auth_token');
          localStorage.removeItem('auth_user');
          navigate('/login', { replace: true });
          return;
        }

        setError(getErrorMessage(err, 'Failed to load entries'));
        setEntries([]);
      } finally {
        setLoading(false);
      }
    };

    fetchEntries();
  }, [page, debouncedSearch, navigate]);

  const handleEdit = (entryId: string) => {
    navigate(`/entries/${entryId}/edit`);
  };

  const handleDelete = async (entryId: string) => {
    if (!window.confirm('Are you sure you want to delete this entry? This action cannot be undone.')) {
      return;
    }

    setDeleting(entryId);
    try {
      await deleteEntry(entryId);
      // Remove from list immediately on success
      setEntries(entries.filter((e) => e.id !== entryId));
      setError(null); // Clear any previous errors
    } catch (err) {
      console.error('[Timeline] Delete failed:', err);

      // Provide specific error messages based on status code
      const apiErr = asApiError(err);
      let message = 'Failed to delete entry';
      if (apiErr.response?.status === 403) {
        message = '⏱️ This entry is read-only and cannot be deleted. Entries become immutable at the end of the day they were created.';
      } else if (apiErr.response?.status === 404) {
        message = 'Entry not found. It may have already been deleted.';
      } else {
        message = getErrorMessage(err, message);
      }

      setError(message);
      console.error('[Timeline] Error message:', message);
    } finally {
      setDeleting(null);
    }
  };

  const handleView = (entryId: string) => {
    navigate(`/entries/${entryId}`);
  };

  const formatDate = (dateString: string): string => {
    return formatEntryDate(dateString);
  };

  const isEditable = (entry: Entry): boolean => {
    return !isEntryImmutable(entry);
  };

  const handleLogout = () => {
    localStorage.removeItem('auth_token');
    localStorage.removeItem('auth_user');
    navigate('/login', { replace: true });
  };

  const getAuthUser = () => {
    const userStr = localStorage.getItem('auth_user');
    if (userStr) {
      try {
        return JSON.parse(userStr);
      } catch {
        return null;
      }
    }
    return null;
  };

  const user = getAuthUser();

  if (loading && entries.length === 0) {
    return (
      <div className="timeline-container">
        <div className="timeline-status loading">Loading entries...</div>
      </div>
    );
  }

  // Render visualization views
  if (viewMode === 'mindmap') {
    return (
      <div className="timeline-container">
        <div className="timeline-header">
          <div className="timeline-header-left">
            <h1>My Journal Entries</h1>
          </div>
          <div className="timeline-header-right">
            {user && (
              <div className="user-info">
                <span className="user-email">👤 {user.email}</span>
                <button
                  className="logout-btn"
                  onClick={handleLogout}
                  title="Logout"
                >
                  🚪 Logout
                </button>
              </div>
            )}
          </div>
        </div>

        <div className="timeline-actions">
          <ViewModeSelector viewMode={viewMode} onChange={setViewMode} />
          <button
            className="create-entry-btn"
            onClick={() => navigate('/entries/new')}
          >
            + New Entry
          </button>
        </div>
        <MindmapView />
      </div>
    );
  }

  if (viewMode === 'sentiment') {
    return (
      <div className="timeline-container">
        <div className="timeline-header">
          <div className="timeline-header-left">
            <h1>My Journal Entries</h1>
          </div>
          <div className="timeline-header-right">
            {user && (
              <div className="user-info">
                <span className="user-email">👤 {user.email}</span>
                <button
                  className="logout-btn"
                  onClick={handleLogout}
                  title="Logout"
                >
                  🚪 Logout
                </button>
              </div>
            )}
          </div>
        </div>

        <div className="timeline-actions">
          <ViewModeSelector viewMode={viewMode} onChange={setViewMode} />
          <button
            className="create-entry-btn"
            onClick={() => navigate('/entries/new')}
          >
            + New Entry
          </button>
        </div>
        <SentimentChart />
      </div>
    );
  }

  // List view (default)
  return (
    <div className="timeline-container">
      <div className="timeline-header">
        <div className="timeline-header-left">
          <h1>My Journal Entries</h1>
        </div>
        <div className="timeline-header-right">
          {user && (
            <div className="user-info">
              <span className="user-email">👤 {user.email}</span>
              <button
                className="logout-btn"
                onClick={handleLogout}
                title="Logout"
              >
                🚪 Logout
              </button>
            </div>
          )}
        </div>
      </div>

      <div className="timeline-actions">
        <ViewModeSelector viewMode={viewMode} onChange={setViewMode} />
        <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
          <button
            className="btn btn-secondary btn-sm"
            onClick={() => handleExport('json')}
            disabled={exporting !== null}
            title="Export all entries as JSON"
          >
            {exporting === 'json' ? 'Exporting…' : '⬇ JSON'}
          </button>
          <button
            className="btn btn-secondary btn-sm"
            onClick={() => handleExport('markdown')}
            disabled={exporting !== null}
            title="Export all entries as Markdown"
          >
            {exporting === 'markdown' ? 'Exporting…' : '⬇ Markdown'}
          </button>
          <button
            className="create-entry-btn"
            onClick={() => navigate('/entries/new')}
          >
            + New Entry
          </button>
        </div>
      </div>

      <input
        type="search"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        placeholder="🔍 Search entries by title or text…"
        aria-label="Search entries"
        style={{
          width: '100%',
          padding: '0.6rem 0.75rem',
          margin: '0.5rem 0 1rem',
          border: '1px solid #ddd',
          borderRadius: '6px',
          fontSize: '1rem',
          boxSizing: 'border-box',
        }}
      />

      {error && (
        <div className="timeline-status error" role="alert">
          {error}
        </div>
      )}

      {entries.length === 0 && !loading ? (
        debouncedSearch ? (
          <div className="empty-state">
            <div className="empty-state-icon">🔍</div>
            <h2 className="empty-state-title">No matches</h2>
            <p className="empty-state-text">No entries match “{debouncedSearch}”.</p>
          </div>
        ) : (
          <div className="empty-state">
            <div className="empty-state-icon">📝</div>
            <h2 className="empty-state-title">No entries yet</h2>
            <p className="empty-state-text">Start writing to create your first journal entry</p>
            <button
              className="create-entry-btn"
              onClick={() => navigate('/entries/new')}
            >
              Create Your First Entry
            </button>
          </div>
        )
      ) : (
        <>
          <div className="entries-list">
            {entries.map((entry) => {
              const immutable = isEntryImmutable(entry);
              const colorScheme = getConfidentialityColor(entry.confidentiality);
              return (
                <article
                  key={entry.id}
                  className={`entry-card ${immutable ? 'immutable' : ''}`}
                >
                  <div className="entry-header">
                    <div className="entry-header-left">
                      <h2 className="entry-title">{entry.title || '[Untitled]'}</h2>
                      <div className="entry-meta">
                        <span className="entry-date">{formatDate(entry.createdAt)}</span>
                        <span
                          className="entry-badge badge-confidentiality"
                          style={{
                            backgroundColor: colorScheme.bgColor,
                            color: colorScheme.textColor,
                          }}
                        >
                          {entry.confidentiality === 'private' ? '🔒 Private' : '🌐 Public'}
                        </span>
                        {immutable && (
                          <span className="entry-badge badge-immutable">
                            🔐 Read-only
                          </span>
                        )}
                      </div>
                    </div>
                  </div>

                  {entry.bodyText && (
                    <div className="entry-body">
                      <p className={`entry-body-text ${entry.bodyText.length > 200 ? 'entry-body-preview' : ''}`}>
                        {entry.bodyText.substring(0, 300)}
                        {entry.bodyText.length > 300 ? '...' : ''}
                      </p>
                    </div>
                  )}

                  {entry.tags && entry.tags.length > 0 && (
                    <div className="entry-tags">
                      {entry.tags.map((tag) => (
                        <span key={tag} className="entry-tag">
                          #{tag}
                        </span>
                      ))}
                    </div>
                  )}

                  <div className="entry-footer">
                    <div className="entry-sentiment">
                      {entry.sentimentScore !== undefined && entry.sentimentScore !== null && (
                        <>
                          <span>Sentiment:</span>
                          <span className="sentiment-score">
                            {entry.sentimentLabel || entry.sentimentScore.toFixed(2)}
                          </span>
                        </>
                      )}
                    </div>
                    <div className="entry-actions">
                      <button
                        className="action-btn"
                        onClick={() => handleView(entry.id)}
                      >
                        👁️ View
                      </button>
                      {isEditable(entry) && (
                        <>
                          <button
                            className="action-btn edit-btn"
                            onClick={() => handleEdit(entry.id)}
                            disabled={deleting === entry.id}
                          >
                            ✏️ Edit
                          </button>
                          <button
                            className="action-btn delete-btn"
                            onClick={() => handleDelete(entry.id)}
                            disabled={deleting === entry.id}
                          >
                            {deleting === entry.id ? '⏳ Deleting...' : '🗑️ Delete'}
                          </button>
                        </>
                      )}
                    </div>
                  </div>
                </article>
              );
            })}
          </div>

          {totalPages > 1 && (
            <div className="timeline-pagination">
              <button
                className="pagination-btn"
                disabled={page === 1 || loading}
                onClick={() => setPage(Math.max(1, page - 1))}
              >
                ← Previous
              </button>

              <span className="pagination-info">
                Page {page} of {totalPages}
              </span>

              <button
                className="pagination-btn"
                disabled={page === totalPages || loading}
                onClick={() => setPage(Math.min(totalPages, page + 1))}
              >
                Next →
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
};

export default Timeline;
