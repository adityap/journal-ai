import React, { useState, useEffect } from 'react';
import { createEntry, updateEntry, getEntry, Entry, CreateEntryRequest, UpdateEntryRequest } from '../services/apiClient';
import { MediaUpload } from './MediaUpload';
import { MediaLibrary } from './MediaLibrary';
import { associateMediaWithEntry, MediaFile } from '../services/mediaClient';

/**
 * Entry Form Component
 * Handles both creating new entries and editing existing ones
 * Includes client-side validation, error handling, and success notifications
 */

interface EntryFormProps {
  entryId?: string; // If present, component is in edit mode
  onSuccess?: () => void;
}

export const EntryForm: React.FC<EntryFormProps> = ({ entryId, onSuccess }) => {
  const [title, setTitle] = useState('');
  const [bodyText, setBodyText] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [tags, setTags] = useState('');
  const [confidentiality, setConfidentiality] = useState('public');
  const [sentimentScore, setSentimentScore] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [validationErrors, setValidationErrors] = useState<string[]>([]);
  const [showMediaUpload, setShowMediaUpload] = useState(false);
  const [selectedMedia, setSelectedMedia] = useState<MediaFile | null>(null);
  const [mediaError, setMediaError] = useState<string | null>(null);

  // Load entry data if editing
  useEffect(() => {
    if (entryId) {
      const fetchEntry = async () => {
        try {
          setLoading(true);
          const entry = await getEntry(entryId);
          setTitle(entry.title || '');
          setBodyText(entry.bodyText || '');
          setCategoryId(entry.categoryId || '');
          setTags(entry.tags?.join(', ') || '');
          setConfidentiality(entry.confidentiality || 'public');
          if (entry.sentimentScore) {
            setSentimentScore(entry.sentimentScore.toString());
          }
          setIsEditing(!isImmutable(entry));
        } catch (err: any) {
          const message = err.response?.data?.message || 'Failed to load entry';
          setError(message);
        } finally {
          setLoading(false);
        }
      };

      fetchEntry();
    }
  }, [entryId]);

  const isImmutable = (entry: Entry): boolean => {
    const now = new Date();
    const readOnlyAfter = new Date(entry.readOnlyAfter);
    return now > readOnlyAfter;
  };

  /**
   * Validate form before submission
   */
  const validateForm = (): boolean => {
    const errors: string[] = [];

    if (!title.trim() && !bodyText.trim()) {
      errors.push('Entry must have either a title or body text');
    }

    if (!confidentiality) {
      errors.push('Confidentiality level is required');
    }

    if (!['public', 'private'].includes(confidentiality)) {
      errors.push('Invalid confidentiality level');
    }

    if (sentimentScore && (isNaN(parseFloat(sentimentScore)) || parseFloat(sentimentScore) < -1 || parseFloat(sentimentScore) > 1)) {
      errors.push('Sentiment score must be between -1 and 1');
    }

    setValidationErrors(errors);
    return errors.length === 0;
  };

  /**
   * Handle form submission
   */
  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    setSuccess(false);

    if (!validateForm()) {
      return;
    }

    setLoading(true);

    try {
      const payload = {
        title: title.trim() || null,
        bodyText: bodyText.trim() || null,
        categoryId: categoryId || null,
        tags: tags
          .split(',')
          .map((t) => t.trim())
          .filter((t) => t.length > 0),
        confidentiality,
        sentimentScore: sentimentScore ? parseFloat(sentimentScore) : null,
        sentimentLabel: null,
        sentimentModel: null,
      };

      if (entryId) {
        // Edit existing entry
        const updatePayload: UpdateEntryRequest = {
          title: (payload.title as string) || undefined,
          bodyText: (payload.bodyText as string) || undefined,
          categoryId: (payload.categoryId as string) || undefined,
          tags: payload.tags,
        };
        await updateEntry(entryId, updatePayload);
      } else {
        // Create new entry
        const createPayload: CreateEntryRequest = payload as CreateEntryRequest;
        const newEntry = await createEntry(createPayload);

        // If media was selected, associate it with the new entry
        if (selectedMedia) {
          try {
            await associateMediaWithEntry(selectedMedia.id, newEntry.id);
          } catch (mediaErr: any) {
            console.error('Failed to associate media with entry:', mediaErr);
            // Continue anyway - entry was created successfully
          }
        }
      }

      setSuccess(true);
      setValidationErrors([]);

      // Clear form
      setTitle('');
      setBodyText('');
      setCategoryId('');
      setTags('');
      setConfidentiality('public');
      setSentimentScore('');

      // Call success callback or redirect
      if (onSuccess) {
        onSuccess();
      } else {
        // Show success message and redirect after 2 seconds
        setTimeout(() => {
          window.location.href = '/';
        }, 2000);
      }
    } catch (err: any) {
      const message = err.response?.data?.message || err.message || 'Failed to save entry';
      
      // Handle validation errors from API
      if (err.response?.data?.errors) {
        const apiErrors: string[] = [];
        Object.values(err.response.data.errors).forEach((fieldErrors: any) => {
          if (Array.isArray(fieldErrors)) {
            apiErrors.push(...fieldErrors);
          }
        });
        setValidationErrors(apiErrors);
      }

      setError(message);
    } finally {
      setLoading(false);
    }
  };

  if (loading && entryId) {
    return <div className="entry-form loading">Loading entry...</div>;
  }

  if (entryId && isEditing === false && !isEditing) {
    return (
      <div className="entry-form">
        <div className="form-alert alert-warning">
          <p>This entry is read-only. It cannot be edited after the end of day it was created.</p>
          <a href="/" className="btn btn-primary">
            Back to Timeline
          </a>
        </div>
      </div>
    );
  }

  return (
    <div className="entry-form">
      <div className="form-header">
        <h1>{entryId ? 'Edit Entry' : 'Create New Entry'}</h1>
      </div>

      {error && (
        <div className="form-alert alert-error" role="alert">
          <p>{error}</p>
        </div>
      )}

      {success && (
        <div className="form-alert alert-success" role="alert">
          <p>Entry saved successfully! Redirecting...</p>
        </div>
      )}

      {validationErrors.length > 0 && (
        <div className="form-alert alert-warning">
          <h3>Please fix the following errors:</h3>
          <ul>
            {validationErrors.map((err, i) => (
              <li key={i}>{err}</li>
            ))}
          </ul>
        </div>
      )}

      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label htmlFor="title">Title (Optional)</label>
          <input
            id="title"
            type="text"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder="Give your entry a title"
            maxLength={500}
            disabled={loading}
          />
          <span className="char-count">{title.length}/500</span>
        </div>

        <div className="form-group">
          <label htmlFor="bodyText">Body Text (Optional)</label>
          <textarea
            id="bodyText"
            value={bodyText}
            onChange={(e) => setBodyText(e.target.value)}
            placeholder="Write your journal entry here..."
            rows={8}
            disabled={loading}
          />
          <span className="char-count">{bodyText.length} characters</span>
        </div>

        <div className="form-row">
          <div className="form-group">
            <label htmlFor="confidentiality">Confidentiality *</label>
            <select
              id="confidentiality"
              value={confidentiality}
              onChange={(e) => setConfidentiality(e.target.value)}
              required
              disabled={loading}
            >
              <option value="public">🌐 Public</option>
              <option value="private">🔒 Private</option>
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="category">Category</label>
            <input
              id="category"
              type="text"
              value={categoryId}
              onChange={(e) => setCategoryId(e.target.value)}
              placeholder="Category ID (optional)"
              disabled={loading}
            />
          </div>
        </div>

        <div className="form-row">
          <div className="form-group">
            <label htmlFor="tags">Tags (comma-separated)</label>
            <input
              id="tags"
              type="text"
              value={tags}
              onChange={(e) => setTags(e.target.value)}
              placeholder="e.g., work, personal, ideas"
              disabled={loading}
            />
          </div>

          <div className="form-group">
            <label htmlFor="sentiment">Sentiment Score (-1 to 1)</label>
            <input
              id="sentiment"
              type="number"
              step="0.1"
              min="-1"
              max="1"
              value={sentimentScore}
              onChange={(e) => setSentimentScore(e.target.value)}
              placeholder="e.g., 0.5"
              disabled={loading}
            />
          </div>
        </div>

        {/* Media Management Section - only show when creating new entry */}
        {!entryId && (
          <div className="media-management-section">
            <div className="media-section-header">
              <h3>Attach Media (Optional)</h3>
              <button
                type="button"
                className="btn btn-secondary btn-sm"
                onClick={() => setShowMediaUpload(!showMediaUpload)}
              >
                {showMediaUpload ? '▼ Hide Upload' : '▶ Show Upload'}
              </button>
            </div>

            {showMediaUpload && (
              <div className="media-upload-wrapper">
                <MediaUpload
                  onSuccess={(media) => {
                    setSelectedMedia(media);
                    setMediaError(null);
                  }}
                  onError={(error) => setMediaError(error)}
                />
              </div>
            )}

            {mediaError && (
              <div className="form-alert alert-error">
                <p>{mediaError}</p>
              </div>
            )}

            {selectedMedia && (
              <div className="selected-media-info">
                <p className="selected-label">
                  ✓ Media selected: <strong>{selectedMedia.fileName}</strong>
                </p>
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
                  onClick={() => setSelectedMedia(null)}
                >
                  Clear Selection
                </button>
              </div>
            )}

            {!showMediaUpload && !selectedMedia && (
              <div className="media-library-wrapper">
                <p className="media-library-label">Or select from your library:</p>
                <MediaLibrary onSelectMedia={(media) => setSelectedMedia(media)} />
              </div>
            )}
          </div>
        )}

        <div className="form-actions">
          <button type="submit" className="btn btn-primary" disabled={loading}>
            {loading ? 'Saving...' : entryId ? 'Update Entry' : 'Create Entry'}
          </button>
          <a href="/" className="btn">
            Cancel
          </a>
        </div>

        <p className="form-help-text">
          * Entry must have either a title or body text. Confidentiality level is required.
          <br />
          Once created, entries become read-only at the end of the day they were created (in your timezone).
        </p>
      </form>

      <style>{`
        .entry-form {
          padding: 2rem;
          max-width: 800px;
          margin: 0 auto;
        }

        .entry-form.loading {
          display: flex;
          align-items: center;
          justify-content: center;
          min-height: 400px;
          font-size: 1.1rem;
          color: #666;
        }

        .form-header {
          margin-bottom: 2rem;
        }

        .form-header h1 {
          font-size: 2rem;
          margin: 0;
          color: #333;
        }

        .form-alert {
          padding: 1rem;
          border-radius: 4px;
          margin-bottom: 1.5rem;
        }

        .form-alert h3 {
          margin-top: 0;
          margin-bottom: 0.5rem;
        }

        .form-alert ul {
          margin: 0;
          padding-left: 1.5rem;
        }

        .form-alert ul li {
          margin-bottom: 0.25rem;
        }

        .alert-error {
          background-color: #ffebee;
          border: 1px solid #f5a5a0;
          color: #c62828;
        }

        .alert-success {
          background-color: #e8f5e9;
          border: 1px solid #a5d6a7;
          color: #2e7d32;
        }

        .alert-warning {
          background-color: #fff3e0;
          border: 1px solid #ffe0b2;
          color: #e65100;
        }

        form {
          background: #fff;
          border: 1px solid #ddd;
          border-radius: 8px;
          padding: 2rem;
        }

        .form-group {
          margin-bottom: 1.5rem;
        }

        .form-row {
          display: grid;
          grid-template-columns: 1fr 1fr;
          gap: 1.5rem;
        }

        label {
          display: block;
          margin-bottom: 0.5rem;
          font-weight: 500;
          color: #333;
        }

        input[type="text"],
        input[type="number"],
        textarea,
        select {
          width: 100%;
          padding: 0.75rem;
          border: 1px solid #ddd;
          border-radius: 4px;
          font-size: 1rem;
          font-family: inherit;
        }

        input[type="text"]:focus,
        input[type="number"]:focus,
        textarea:focus,
        select:focus {
          outline: none;
          border-color: #1976d2;
          box-shadow: 0 0 0 3px rgba(25, 118, 210, 0.1);
        }

        input:disabled,
        textarea:disabled,
        select:disabled {
          background-color: #f5f5f5;
          color: #999;
          cursor: not-allowed;
        }

        textarea {
          resize: vertical;
          min-height: 200px;
        }

        .char-count {
          display: block;
          font-size: 0.85rem;
          color: #999;
          margin-top: 0.25rem;
        }

        .media-management-section {
          margin-top: 2rem;
          padding: 1.5rem;
          background-color: #f9f9f9;
          border: 1px solid #e0e0e0;
          border-radius: 8px;
        }

        .media-section-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 1rem;
        }

        .media-section-header h3 {
          margin: 0;
          font-size: 1.1rem;
          color: #333;
        }

        .btn-sm {
          padding: 0.5rem 1rem;
          font-size: 0.9rem;
        }

        .media-upload-wrapper {
          margin-bottom: 1.5rem;
        }

        .selected-media-info {
          padding: 1rem;
          background-color: #e8f5e9;
          border: 1px solid #c8e6c9;
          border-radius: 4px;
          margin-bottom: 1rem;
        }

        .selected-label {
          margin: 0 0 0.5rem 0;
          color: #2e7d32;
          font-size: 0.95rem;
        }

        .media-library-wrapper {
          margin-top: 1rem;
          padding-top: 1rem;
          border-top: 1px solid #ddd;
        }

        .media-library-label {
          margin: 0 0 1rem 0;
          color: #666;
          font-size: 0.9rem;
        }

        .form-actions {
          display: flex;
          gap: 1rem;
          margin-top: 2rem;
          padding-top: 2rem;
          border-top: 1px solid #ddd;
        }

        .btn {
          padding: 0.75rem 1.5rem;
          border: 1px solid #ddd;
          border-radius: 4px;
          background-color: #f5f5f5;
          color: #333;
          cursor: pointer;
          font-size: 1rem;
          text-decoration: none;
          display: inline-block;
          text-align: center;
          transition: background-color 0.2s;
        }

        .btn:hover:not(:disabled) {
          background-color: #e0e0e0;
        }

        .btn:disabled {
          opacity: 0.5;
          cursor: not-allowed;
        }

        .btn-primary {
          background-color: #1976d2;
          color: #fff;
          border-color: #1976d2;
        }

        .btn-primary:hover {
          background-color: #1565c0;
        }

        .form-help-text {
          font-size: 0.85rem;
          color: #666;
          margin-top: 1rem;
          padding: 1rem;
          background-color: #f9f9f9;
          border-radius: 4px;
          line-height: 1.6;
        }

        @media (max-width: 600px) {
          .entry-form {
            padding: 1rem;
          }

          form {
            padding: 1rem;
          }

          .form-row {
            grid-template-columns: 1fr;
          }

          .form-actions {
            flex-direction: column;
          }

          .btn {
            width: 100%;
          }
        }
      `}</style>
    </div>
  );
};

export default EntryForm;
