import React, { useEffect, useState } from 'react';
import { listEntries, Entry, PagedResult } from '../services/apiClient';

/**
 * Timeline Component
 * Displays a list of journal entries with pagination, confidentiality badges,
 * immutability indicators, and action buttons for edit/delete
 */

export const Timeline: React.FC = () => {
  const [entries, setEntries] = useState<Entry[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

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
    // Navigate to edit page
    window.location.href = `/entries/${entryId}/edit`;
  };

  const handleDelete = (entryId: string) => {
    if (confirm('Are you sure you want to delete this entry?')) {
      // Delete will be implemented in T111 or separate delete handler
      console.log('Delete entry:', entryId);
    }
  };

  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const isEditable = (entry: Entry): boolean => {
    const now = new Date();
    const readOnlyAfter = new Date(entry.readOnlyAfter);
    return now <= readOnlyAfter;
  };

  if (loading && entries.length === 0) {
    return <div className="timeline loading">Loading entries...</div>;
  }

  return (
    <div className="timeline">
      <div className="timeline-header">
        <h1>My Journal Entries</h1>
      </div>

      {error && (
        <div className="timeline-error" role="alert">
          <p>{error}</p>
        </div>
      )}

      {entries.length === 0 && !loading && (
        <div className="timeline-empty">
          <p>No entries yet. Start writing to create your first entry!</p>
          <a href="/entries/new" className="btn btn-primary">
            Create Entry
          </a>
        </div>
      )}

      {entries.length > 0 && (
        <>
          <div className="timeline-list">
            {entries.map((entry) => (
              <article key={entry.id} className="entry-card">
                <div className="entry-header">
                  <h2>{entry.title || 'Untitled'}</h2>
                  <div className="entry-badges">
                    <span className={`badge confidentiality ${entry.confidentiality}`}>
                      {entry.confidentiality === 'private' ? '🔒 Private' : '🌐 Public'}
                    </span>
                    {!isEditable(entry) && (
                      <span className="badge immutable">
                        🔐 Read-only
                      </span>
                    )}
                  </div>
                </div>

                <div className="entry-meta">
                  <span className="created-at">
                    Created: {formatDate(entry.createdAt)}
                  </span>
                  <span className="read-only-after">
                    Editable until: {formatDate(entry.readOnlyAfter)}
                  </span>
                </div>

                <p className="entry-body">
                  {entry.bodyText ? entry.bodyText.substring(0, 200) : 'No content'}
                  {entry.bodyText && entry.bodyText.length > 200 ? '...' : ''}
                </p>

                {entry.tags && entry.tags.length > 0 && (
                  <div className="entry-tags">
                    {entry.tags.map((tag) => (
                      <span key={tag} className="tag">
                        #{tag}
                      </span>
                    ))}
                  </div>
                )}

                <div className="entry-actions">
                  <button
                    className="btn btn-small"
                    onClick={() => window.location.href = `/entries/${entry.id}`}
                  >
                    View
                  </button>

                  {isEditable(entry) && (
                    <>
                      <button
                        className="btn btn-small btn-edit"
                        onClick={() => handleEdit(entry.id)}
                      >
                        Edit
                      </button>
                      <button
                        className="btn btn-small btn-danger"
                        onClick={() => handleDelete(entry.id)}
                      >
                        Delete
                      </button>
                    </>
                  )}
                </div>
              </article>
            ))}
          </div>

          {totalPages > 1 && (
            <div className="timeline-pagination">
              <button
                disabled={page === 1}
                onClick={() => setPage(page - 1)}
                className="btn"
              >
                ← Previous
              </button>

              <span className="pagination-info">
                Page {page} of {totalPages}
              </span>

              <button
                disabled={page === totalPages}
                onClick={() => setPage(page + 1)}
                className="btn"
              >
                Next →
              </button>
            </div>
          )}
        </>
      )}

      <style>{`
        .timeline {
          padding: 2rem;
          max-width: 900px;
          margin: 0 auto;
        }

        .timeline.loading {
          display: flex;
          align-items: center;
          justify-content: center;
          min-height: 400px;
          font-size: 1.1rem;
          color: #666;
        }

        .timeline-header {
          margin-bottom: 2rem;
        }

        .timeline-header h1 {
          font-size: 2rem;
          margin: 0;
          color: #333;
        }

        .timeline-error {
          background-color: #fee;
          border: 1px solid #fcc;
          border-radius: 4px;
          padding: 1rem;
          margin-bottom: 1rem;
          color: #c33;
        }

        .timeline-empty {
          text-align: center;
          padding: 3rem 1rem;
          color: #666;
        }

        .timeline-empty .btn {
          margin-top: 1rem;
        }

        .timeline-list {
          display: flex;
          flex-direction: column;
          gap: 1.5rem;
          margin-bottom: 2rem;
        }

        .entry-card {
          border: 1px solid #ddd;
          border-radius: 8px;
          padding: 1.5rem;
          background: #fff;
          box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
          transition: box-shadow 0.2s;
        }

        .entry-card:hover {
          box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
        }

        .entry-header {
          display: flex;
          justify-content: space-between;
          align-items: flex-start;
          margin-bottom: 1rem;
          gap: 1rem;
        }

        .entry-header h2 {
          margin: 0;
          font-size: 1.5rem;
          color: #333;
          flex: 1;
          word-break: break-word;
        }

        .entry-badges {
          display: flex;
          gap: 0.5rem;
          flex-wrap: wrap;
          white-space: nowrap;
        }

        .badge {
          display: inline-block;
          padding: 0.25rem 0.75rem;
          border-radius: 20px;
          font-size: 0.85rem;
          font-weight: 500;
        }

        .badge.confidentiality {
          background-color: #e3f2fd;
          color: #1976d2;
        }

        .badge.confidentiality.private {
          background-color: #ffe0b2;
          color: #f57c00;
        }

        .badge.immutable {
          background-color: #f3e5f5;
          color: #7b1fa2;
        }

        .entry-meta {
          display: flex;
          gap: 1rem;
          font-size: 0.9rem;
          color: #666;
          margin-bottom: 1rem;
          flex-wrap: wrap;
        }

        .entry-body {
          margin: 1rem 0;
          color: #555;
          line-height: 1.6;
        }

        .entry-tags {
          display: flex;
          gap: 0.5rem;
          flex-wrap: wrap;
          margin: 1rem 0;
        }

        .tag {
          background-color: #f0f0f0;
          color: #666;
          padding: 0.25rem 0.75rem;
          border-radius: 4px;
          font-size: 0.85rem;
        }

        .entry-actions {
          display: flex;
          gap: 0.5rem;
          margin-top: 1rem;
          flex-wrap: wrap;
        }

        .btn {
          padding: 0.5rem 1rem;
          border: 1px solid #ddd;
          border-radius: 4px;
          background-color: #f5f5f5;
          color: #333;
          cursor: pointer;
          font-size: 0.9rem;
          transition: background-color 0.2s;
        }

        .btn:hover:not(:disabled) {
          background-color: #e0e0e0;
        }

        .btn:disabled {
          opacity: 0.5;
          cursor: not-allowed;
        }

        .btn-small {
          padding: 0.35rem 0.75rem;
          font-size: 0.85rem;
        }

        .btn-edit {
          background-color: #e3f2fd;
          color: #1976d2;
          border-color: #1976d2;
        }

        .btn-edit:hover {
          background-color: #bbdefb;
        }

        .btn-danger {
          background-color: #ffebee;
          color: #c62828;
          border-color: #c62828;
        }

        .btn-danger:hover {
          background-color: #ffcdd2;
        }

        .btn-primary {
          background-color: #1976d2;
          color: #fff;
          border-color: #1976d2;
        }

        .btn-primary:hover {
          background-color: #1565c0;
        }

        .timeline-pagination {
          display: flex;
          justify-content: center;
          align-items: center;
          gap: 1rem;
          margin-top: 2rem;
        }

        .pagination-info {
          color: #666;
          font-size: 0.9rem;
        }

        @media (max-width: 600px) {
          .timeline {
            padding: 1rem;
          }

          .entry-header {
            flex-direction: column;
          }

          .entry-badges {
            width: 100%;
          }

          .entry-actions {
            width: 100%;
          }

          .btn {
            flex: 1;
            text-align: center;
          }
        }
      `}</style>
    </div>
  );
};

export default Timeline;
