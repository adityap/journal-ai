import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { entriesClient, Entry } from '../services/entriesClient';
import { listCategories, buildCategoryNameMap } from '../services/categoriesClient';
import { getErrorMessage } from '../utils/errors';
import { UnlockModal } from './UnlockModal';
import '../styles/entry.css';

interface MediaFile {
  id: string;
  entryId: string;
  fileName: string;
  mediaType: string;
  url?: string;
  uploadedAt: string;
}

export const EntryDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [entry, setEntry] = useState<Entry | null>(null);
  const [media, setMedia] = useState<MediaFile[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [categoryNames, setCategoryNames] = useState<Record<string, string>>({});
  const [showUnlock, setShowUnlock] = useState(false);

  useEffect(() => {
    loadEntry();
    listCategories()
      .then((cats) => setCategoryNames(buildCategoryNameMap(cats)))
      .catch(() => setCategoryNames({}));
    // Intentionally runs only when the entry id changes; loadEntry reads `id`.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const loadEntry = async () => {
    if (!id) return;
    
    try {
      setLoading(true);
      setError(null);
      
      // Load entry
      const entryData = await entriesClient.getEntry(id);
      setEntry(entryData);

      // Load media if any exist
      try {
        const mediaResponse = await fetch(
          `${import.meta.env.VITE_API_URL || 'http://localhost:5000'}/api/entries/${id}/media`,
          {
            headers: {
              Authorization: `Bearer ${localStorage.getItem('token')}`
            }
          }
        );
        if (mediaResponse.ok) {
          const mediaData = await mediaResponse.json();
          setMedia(mediaData);
        }
      } catch (err) {
        // Media loading is optional
        console.error('Failed to load media:', err);
      }
    } catch (err) {
      setError(getErrorMessage(err, 'Failed to load entry'));
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async () => {
    if (!entry || !window.confirm('Are you sure you want to delete this entry?')) {
      return;
    }

    try {
      await entriesClient.deleteEntry(entry.id);
      navigate('/');
    } catch (err) {
      setError(getErrorMessage(err, 'Failed to delete entry'));
    }
  };

  const handleEdit = () => {
    navigate(`/entries/${id}/edit`);
  };

  const formatDate = (dateString: string) => {
    try {
      const date = new Date(dateString);
      return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
      });
    } catch {
      return dateString;
    }
  };

  const getSentimentEmoji = (score?: number) => {
    if (!score) return '';
    if (score >= 0.7) return '😊';
    if (score >= 0.4) return '😐';
    return '😟';
  };

  const confidentialityColors: Record<string, string> = {
    public: '#667eea',
    private: '#f093fb',
    confidential: '#ff6b6b'
  };

  if (loading) {
    return (
      <div className="entry-detail-container">
        <div className="loading">Loading entry...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="entry-detail-container">
        <div className="error-message">
          <p>❌ {error}</p>
          <button onClick={() => navigate('/')} className="btn btn-primary">
            Back to Entries
          </button>
        </div>
      </div>
    );
  }

  if (!entry) {
    return (
      <div className="entry-detail-container">
        <div className="error-message">
          <p>Entry not found</p>
          <button onClick={() => navigate('/')} className="btn btn-primary">
            Back to Entries
          </button>
        </div>
      </div>
    );
  }

  const isImmutable = entriesClient.isEntryImmutable(entry);
  const confidentialColor = confidentialityColors[entry.confidentiality || 'public'];

  return (
    <div className="entry-detail-container">
      <div className="entry-detail-header">
        <button onClick={() => navigate('/')} className="btn btn-secondary">
          ← Back
        </button>
        <div className="entry-detail-actions">
          {!isImmutable && !entry.locked && (
            <>
              <button onClick={handleEdit} className="btn btn-primary">
                ✏️ Edit
              </button>
              <button onClick={handleDelete} className="btn btn-danger">
                🗑️ Delete
              </button>
            </>
          )}
        </div>
      </div>

      <div className="entry-detail-content">
        <h1>{entry.locked ? '🔒 Private entry (locked)' : entry.title}</h1>

        <div className="entry-detail-metadata">
          <span className="metadata-item">
            📅 {formatDate(entry.createdAt)}
          </span>
          {entry.categoryId && (
            <span className="metadata-item">
              📁 {categoryNames[entry.categoryId] || 'Uncategorized'}
            </span>
          )}
          <span
            className="metadata-item confidentiality-badge"
            style={{ backgroundColor: confidentialColor }}
          >
            🔒 {entry.confidentiality || 'public'}
          </span>
          {entry.sentimentScore && (
            <span className="metadata-item">
              {getSentimentEmoji(entry.sentimentScore)} Sentiment:{' '}
              {(entry.sentimentScore * 100).toFixed(0)}%
            </span>
          )}
        </div>

        {isImmutable && (
          <div className="read-only-notice">
            🔒 This entry is read-only and cannot be modified.
          </div>
        )}

        {entry.locked ? (
          <div className="read-only-notice">
            🔒 This private entry is locked.{' '}
            <button onClick={() => setShowUnlock(true)} className="btn btn-primary btn-sm">
              🔓 Unlock to view
            </button>
          </div>
        ) : (
          <div className="entry-detail-body">
            {entry.bodyText}
          </div>
        )}

        {entry.tags && entry.tags.length > 0 && (
          <div className="entry-detail-tags">
            <strong>Tags:</strong>
            <div className="tag-list">
              {entry.tags.map((tag, index) => (
                <span key={index} className="tag-badge">
                  #{tag}
                </span>
              ))}
            </div>
          </div>
        )}

        {media.length > 0 && (
          <div className="entry-detail-media">
            <h3>📎 Attachments ({media.length})</h3>
            <div className="media-list">
              {media.map((file) => (
                <div key={file.id} className="media-item">
                  <div className="media-info">
                    <strong>{file.fileName}</strong>
                    <small>{file.mediaType}</small>
                  </div>
                  {file.url && (
                    <a href={file.url} target="_blank" rel="noopener noreferrer" className="btn btn-sm btn-primary">
                      Download
                    </a>
                  )}
                </div>
              ))}
            </div>
          </div>
        )}
      </div>

      {showUnlock && (
        <UnlockModal
          onUnlocked={() => {
            setShowUnlock(false);
            loadEntry();
          }}
          onClose={() => setShowUnlock(false)}
        />
      )}
    </div>
  );
};
