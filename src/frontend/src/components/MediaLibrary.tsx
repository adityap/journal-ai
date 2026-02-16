import React, { useEffect, useState } from 'react';
import { listMediaFiles, deleteMediaFile, MediaFile } from '../services/mediaClient';

/**
 * Media Library Component
 * Displays all uploaded media files with options to view, delete, and associate with entries
 * Supports pagination and filtering
 */

interface MediaLibraryProps {
  entryId?: string;
  onSelectMedia?: (media: MediaFile) => void;
  showAssociate?: boolean;
}

export const MediaLibrary: React.FC<MediaLibraryProps> = ({
  entryId,
  onSelectMedia,
  showAssociate = false,
}) => {
  const [mediaFiles, setMediaFiles] = useState<MediaFile[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedMediaId, setSelectedMediaId] = useState<string | null>(null);

  // Fetch media files on page change
  useEffect(() => {
    fetchMediaFiles();
  }, [page]);

  const fetchMediaFiles = async () => {
    setLoading(true);
    setError(null);

    try {
      const result = await listMediaFiles(page, 12, entryId);
      setMediaFiles(result.items);
      setTotalPages(result.totalPages);
    } catch (err: any) {
      const message = err.response?.data?.message || 'Failed to load media';
      setError(message);
      setMediaFiles([]);
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (mediaId: string) => {
    if (!confirm('Are you sure you want to delete this media?')) {
      return;
    }

    try {
      await deleteMediaFile(mediaId);
      setMediaFiles(mediaFiles.filter((m) => m.id !== mediaId));
    } catch (err: any) {
      const message = err.response?.data?.message || 'Failed to delete media';
      setError(message);
    }
  };

  const handleSelect = (media: MediaFile) => {
    setSelectedMediaId(media.id);
    onSelectMedia?.(media);
  };

  const getMediaTypeIcon = (mimeType: string): string => {
    if (mimeType.startsWith('image/')) return '🖼️';
    if (mimeType.startsWith('video/')) return '🎥';
    if (mimeType.includes('pdf')) return '📄';
    return '📎';
  };

  const getMediaTypeName = (mimeType: string): string => {
    if (mimeType.startsWith('image/')) return 'Image';
    if (mimeType.startsWith('video/')) return 'Video';
    if (mimeType.includes('pdf')) return 'PDF';
    return 'Media';
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  };

  const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  if (loading && mediaFiles.length === 0) {
    return (
      <div className="media-library-container">
        <div className="loading">Loading media...</div>
      </div>
    );
  }

  if (error && mediaFiles.length === 0) {
    return (
      <div className="media-library-container">
        <div className="error-message">{error}</div>
      </div>
    );
  }

  if (mediaFiles.length === 0) {
    return (
      <div className="media-library-container">
        <div className="empty-state">
          <p>No media files yet</p>
          <p className="empty-hint">Upload a file to get started</p>
        </div>
      </div>
    );
  }

  return (
    <div className="media-library-container">
      <div className="media-grid">
        {mediaFiles.map((media) => (
          <div
            key={media.id}
            className={`media-card ${selectedMediaId === media.id ? 'selected' : ''}`}
            onClick={() => handleSelect(media)}
          >
            <div className="media-preview">
              {media.thumbnailUrl ? (
                <img src={media.thumbnailUrl} alt={media.fileName} />
              ) : (
                <div className="media-placeholder">
                  <span className="media-icon">{getMediaTypeIcon(media.mimeType)}</span>
                  <span className="media-type">{getMediaTypeName(media.mimeType)}</span>
                </div>
              )}
            </div>

            <div className="media-info">
              <h4 className="media-name" title={media.fileName}>
                {media.fileName}
              </h4>
              <p className="media-meta">
                {formatFileSize(media.size)} • {formatDate(media.createdAt)}
              </p>
              {media.associatedEntryId && (
                <p className="media-associated">
                  📎 Associated with entry
                </p>
              )}
            </div>

            <div className="media-actions">
              {showAssociate && !media.associatedEntryId && (
                <button
                  type="button"
                  className="action-btn associate-btn"
                  title="Associate with entry"
                  onClick={(e) => {
                    e.stopPropagation();
                    handleSelect(media);
                  }}
                >
                  📎
                </button>
              )}
              {media.presignedDownloadUrl && (
                <a
                  href={media.presignedDownloadUrl}
                  className="action-btn download-btn"
                  title="Download"
                  onClick={(e) => e.stopPropagation()}
                  download
                >
                  ⬇️
                </a>
              )}
              <button
                type="button"
                className="action-btn delete-btn"
                title="Delete"
                onClick={(e) => {
                  e.stopPropagation();
                  handleDelete(media.id);
                }}
              >
                🗑️
              </button>
            </div>
          </div>
        ))}
      </div>

      {totalPages > 1 && (
        <div className="pagination">
          <button
            className="btn btn-secondary"
            onClick={() => setPage(Math.max(1, page - 1))}
            disabled={page === 1}
          >
            ← Previous
          </button>

          <span className="page-info">
            Page {page} of {totalPages}
          </span>

          <button
            className="btn btn-secondary"
            onClick={() => setPage(Math.min(totalPages, page + 1))}
            disabled={page === totalPages}
          >
            Next →
          </button>
        </div>
      )}
    </div>
  );
};

export default MediaLibrary;
