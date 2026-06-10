import React, { useEffect, useRef, useState } from 'react';
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
import { RelatedGraph } from './RelatedGraph';
import {
  downloadExport,
  ExportFormat,
  createExportJob,
  listExportJobs,
  downloadExportJob,
  ExportJob,
} from '../services/exportClient';
import { importEntriesFromFile } from '../services/importClient';
import { isUnlocked } from '../services/unlockClient';
import { UnlockModal } from './UnlockModal';
import { asApiError, getErrorMessage } from '../utils/errors';
import '../styles/entry.css';

type ViewMode = 'list' | 'mindmap' | 'sentiment' | 'related';

const VIEW_MODES: { mode: ViewMode; label: string; title: string }[] = [
  { mode: 'list', label: '📋 List', title: 'List View' },
  { mode: 'mindmap', label: '🧠 Mindmap', title: 'Mindmap View' },
  { mode: 'sentiment', label: '📈 Sentiment', title: 'Sentiment Trends' },
  { mode: 'related', label: '🔗 Related', title: 'Related entries (text similarity)' },
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
  const [importing, setImporting] = useState(false);
  const [info, setInfo] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [showUnlock, setShowUnlock] = useState(false);
  const [exportJobs, setExportJobs] = useState<ExportJob[]>([]);
  const [showJobs, setShowJobs] = useState(false);
  const importInputRef = useRef<HTMLInputElement>(null);

  const hasLockedEntries = entries.some((e) => e.locked);
  const hasPendingJobs = exportJobs.some((j) => j.status === 'queued' || j.status === 'running');

  const refreshJobs = async () => {
    try {
      setExportJobs(await listExportJobs());
    } catch {
      /* non-fatal: the panel just won't update */
    }
  };

  const handleCreateZipJob = async () => {
    setError(null);
    setShowJobs(true);
    try {
      await createExportJob('zip');
      await refreshJobs();
    } catch (err) {
      setError(getErrorMessage(err, 'Could not start export'));
    }
  };

  const handleDownloadJob = async (job: ExportJob) => {
    try {
      await downloadExportJob(job);
    } catch (err) {
      setError(getErrorMessage(err, 'Download failed'));
    }
  };

  // While the panel is open and any job is still running, poll for status.
  useEffect(() => {
    if (!showJobs || !hasPendingJobs) return;
    const handle = setInterval(refreshJobs, 3000);
    return () => clearInterval(handle);
  }, [showJobs, hasPendingJobs]);

  const handleUnlocked = () => {
    setShowUnlock(false);
    setInfo('Private entries unlocked for this session.');
    setReloadKey((k) => k + 1);
  };

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

  const handleImportFile = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    // Reset the input so picking the same file again re-triggers onChange.
    e.target.value = '';
    if (!file) return;

    setImporting(true);
    setError(null);
    setInfo(null);
    try {
      const result = await importEntriesFromFile(file);
      const parts = [`Imported ${result.imported}`];
      if (result.skippedDuplicates > 0) parts.push(`skipped ${result.skippedDuplicates} duplicate(s)`);
      if (result.failed > 0) parts.push(`${result.failed} failed`);
      setInfo(parts.join(', ') + '.');
      // Reload the list so newly imported entries appear.
      setPage(1);
      setReloadKey((k) => k + 1);
    } catch (err) {
      setError(getErrorMessage(err, 'Import failed'));
    } finally {
      setImporting(false);
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
  }, [page, debouncedSearch, navigate, reloadKey]);

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

  if (viewMode === 'related') {
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
        <RelatedGraph />
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
            className="btn btn-secondary btn-sm"
            onClick={handleCreateZipJob}
            title="Export a ZIP bundling entries and media (runs in the background)"
          >
            ⬇ ZIP (media)
          </button>
          <button
            className="btn btn-secondary btn-sm"
            onClick={() => { setShowJobs((v) => !v); if (!showJobs) refreshJobs(); }}
            title="Show export jobs"
          >
            {showJobs ? 'Hide jobs' : 'Export jobs'}
          </button>
          <button
            className="btn btn-secondary btn-sm"
            onClick={() => importInputRef.current?.click()}
            disabled={importing}
            title="Import entries from a JSON export"
          >
            {importing ? 'Importing…' : '⬆ Import'}
          </button>
          <input
            ref={importInputRef}
            type="file"
            accept="application/json,.json"
            onChange={handleImportFile}
            style={{ display: 'none' }}
            aria-hidden="true"
          />
          {hasLockedEntries && !isUnlocked() && (
            <button
              className="btn btn-secondary btn-sm"
              onClick={() => setShowUnlock(true)}
              title="Unlock private entries with your account password"
            >
              🔓 Unlock
            </button>
          )}
          <button
            className="create-entry-btn"
            onClick={() => navigate('/entries/new')}
          >
            + New Entry
          </button>
        </div>
      </div>

      {showJobs && (
        <div
          style={{
            border: '1px solid #e0e0e0', borderRadius: '6px', padding: '0.75rem 1rem',
            margin: '0 0 1rem', background: '#fafafa',
          }}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
            <strong>Export jobs</strong>
            <button className="btn btn-secondary btn-sm" onClick={refreshJobs}>↻ Refresh</button>
          </div>
          {exportJobs.length === 0 ? (
            <p style={{ margin: 0, color: '#777', fontSize: '0.9rem' }}>No export jobs yet.</p>
          ) : (
            <ul style={{ listStyle: 'none', margin: 0, padding: 0 }}>
              {exportJobs.map((job) => (
                <li
                  key={job.id}
                  style={{
                    display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                    padding: '0.4rem 0', borderTop: '1px solid #eee', fontSize: '0.9rem',
                  }}
                >
                  <span>
                    {job.format.toUpperCase()} ·{' '}
                    {job.status === 'completed' ? '✅ ready'
                      : job.status === 'failed' ? `❌ ${job.error || 'failed'}`
                      : `⏳ ${job.status}`}{' '}
                    <span style={{ color: '#999' }}>{formatDate(job.createdAt)}</span>
                  </span>
                  {job.ready && (
                    <button className="btn btn-primary btn-sm" onClick={() => handleDownloadJob(job)}>
                      ⬇ Download
                    </button>
                  )}
                </li>
              ))}
            </ul>
          )}
        </div>
      )}

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

      {info && (
        <div className="timeline-status" role="status">
          {info}
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
                      <h2 className="entry-title">
                        {entry.locked ? '🔒 Private entry (locked)' : entry.title || '[Untitled]'}
                      </h2>
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

                  {entry.locked ? (
                    <div className="entry-body">
                      <p className="entry-body-text" style={{ color: '#888', fontStyle: 'italic' }}>
                        This entry is private. Unlock to view its contents.{' '}
                        <button
                          className="action-btn"
                          onClick={() => setShowUnlock(true)}
                        >
                          🔓 Unlock
                        </button>
                      </p>
                    </div>
                  ) : (
                    entry.bodyText && (
                      <div className="entry-body">
                        <p className={`entry-body-text ${entry.bodyText.length > 200 ? 'entry-body-preview' : ''}`}>
                          {entry.bodyText.substring(0, 300)}
                          {entry.bodyText.length > 300 ? '...' : ''}
                        </p>
                      </div>
                    )
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
                      {isEditable(entry) && !entry.locked && (
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

      {showUnlock && (
        <UnlockModal onUnlocked={handleUnlocked} onClose={() => setShowUnlock(false)} />
      )}
    </div>
  );
};

export default Timeline;
