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
import '../styles/entry.css';

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
  const [viewMode, setViewMode] = useState<'list' | 'mindmap' | 'sentiment'>('list');

  // Load entries on page change
  useEffect(() => {
    const fetchEntries = async () => {
      setLoading(true);
      setError(null);

      try {
        const result: PagedResult<Entry> = await listEntries(page, 20);
        setEntries(result.items);
        setTotalPages(result.totalPages);
      } catch (err: any) {
        const message = err.response?.data?.message || err.message || 'Failed to load entries';
        setError(message);
        setEntries([]);
      } finally {
        setLoading(false);
      }
    };

    fetchEntries();
  }, [page]);

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
      setEntries(entries.filter((e) => e.id !== entryId));
    } catch (err: any) {
      const message = err.response?.data?.message || 'Failed to delete entry';
      setError(message);
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
          <h1>My Journal Entries</h1>
          <div className="timeline-actions">
            <div className="view-mode-selector">
              <button
                className={`view-btn ${viewMode === 'list' ? 'active' : ''}`}
                onClick={() => setViewMode('list')}
                title="List View"
              >
                📋 List
              </button>
              <button
                className={`view-btn ${viewMode === 'mindmap' ? 'active' : ''}`}
                onClick={() => setViewMode('mindmap')}
                title="Mindmap View"
              >
                🧠 Mindmap
              </button>
              <button
                className={`view-btn ${viewMode === 'sentiment' ? 'active' : ''}`}
                onClick={() => setViewMode('sentiment')}
                title="Sentiment Trends"
              >
                📈 Sentiment
              </button>
            </div>
            <button
              className="create-entry-btn"
              onClick={() => navigate('/entries/new')}
            >
              + New Entry
            </button>
          </div>
        </div>
        <MindmapView />
      </div>
    );
  }

  if (viewMode === 'sentiment') {
    return (
      <div className="timeline-container">
        <div className="timeline-header">
          <h1>My Journal Entries</h1>
          <div className="timeline-actions">
            <div className="view-mode-selector">
              <button
                className={`view-btn ${viewMode === 'list' ? 'active' : ''}`}
                onClick={() => setViewMode('list')}
                title="List View"
              >
                📋 List
              </button>
              <button
                className={`view-btn ${viewMode === 'mindmap' ? 'active' : ''}`}
                onClick={() => setViewMode('mindmap')}
                title="Mindmap View"
              >
                🧠 Mindmap
              </button>
              <button
                className={`view-btn ${viewMode === 'sentiment' ? 'active' : ''}`}
                onClick={() => setViewMode('sentiment')}
                title="Sentiment Trends"
              >
                📈 Sentiment
              </button>
            </div>
            <button
              className="create-entry-btn"
              onClick={() => navigate('/entries/new')}
            >
              + New Entry
            </button>
          </div>
        </div>
        <SentimentChart />
      </div>
    );
  }

  // List view (default)
  return (
    <div className="timeline-container">
      <div className="timeline-header">
        <h1>My Journal Entries</h1>
        <div className="timeline-actions">
          <div className="view-mode-selector">
            <button
              className={`view-btn ${viewMode === 'list' ? 'active' : ''}`}
              onClick={() => setViewMode('list')}
              title="List View"
            >
              📋 List
            </button>
            <button
              className={`view-btn ${viewMode === 'mindmap' ? 'active' : ''}`}
              onClick={() => setViewMode('mindmap')}
              title="Mindmap View"
            >
              🧠 Mindmap
            </button>
            <button
              className={`view-btn ${viewMode === 'sentiment' ? 'active' : ''}`}
              onClick={() => setViewMode('sentiment')}
              title="Sentiment Trends"
            >
              📈 Sentiment
            </button>
          </div>
          <button
            className="create-entry-btn"
            onClick={() => navigate('/entries/new')}
          >
            + New Entry
          </button>
        </div>
      </div>

      {error && (
        <div className="timeline-status error" role="alert">
          {error}
        </div>
      )}

      {entries.length === 0 && !loading ? (
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
