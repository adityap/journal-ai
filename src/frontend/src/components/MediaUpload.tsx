import React, { useRef, useState } from 'react';
import {
  initiateMediaUpload,
  completeMediaUpload,
  uploadFileToS3,
  MediaFile,
} from '../services/mediaClient';

/**
 * Media Upload Component
 * Provides drag-and-drop and file selection for uploading media to S3
 * Shows progress tracking and handles errors gracefully
 */

interface MediaUploadProps {
  onSuccess?: (media: MediaFile) => void;
  onError?: (error: string) => void;
  maxFileSize?: number; // in bytes, default 100MB
}

export const MediaUpload: React.FC<MediaUploadProps> = ({
  onSuccess,
  onError,
  maxFileSize = 100 * 1024 * 1024,
}) => {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [fileName, setFileName] = useState<string | null>(null);

  const ALLOWED_MIME_TYPES = [
    'image/jpeg',
    'image/png',
    'image/gif',
    'image/webp',
    'video/mp4',
    'video/webm',
    'application/pdf',
  ];

  const validateFile = (file: File): { valid: boolean; error?: string } => {
    if (!ALLOWED_MIME_TYPES.includes(file.type)) {
      return {
        valid: false,
        error: `File type not supported. Allowed types: images (JPEG, PNG, GIF, WebP), videos (MP4, WebM), and PDFs.`,
      };
    }

    if (file.size > maxFileSize) {
      return {
        valid: false,
        error: `File is too large. Maximum size: ${(maxFileSize / 1024 / 1024).toFixed(0)}MB`,
      };
    }

    return { valid: true };
  };

  const handleUpload = async (file: File) => {
    const validation = validateFile(file);
    if (!validation.valid) {
      setError(validation.error || 'Invalid file');
      onError?.(validation.error || 'Invalid file');
      return;
    }

    setIsUploading(true);
    setUploadProgress(0);
    setError(null);
    setFileName(file.name);

    try {
      // Step 1: Initiate upload and get presigned URL
      const initiateResponse = await initiateMediaUpload(file.name, file.type, file.size);

      // Step 2: Upload file to S3 using presigned URL
      await uploadFileToS3(initiateResponse.presignedUrl, file, (progress) => {
        setUploadProgress(progress);
      });

      // Step 3: Complete upload and create media record
      const mediaFile = await completeMediaUpload(
        initiateResponse.mediaId,
        initiateResponse.uploadId
      );

      setFileName(null);
      setUploadProgress(0);
      setIsUploading(false);

      onSuccess?.(mediaFile);
    } catch (err: any) {
      const errorMessage = err.response?.data?.message || err.message || 'Upload failed';
      setError(errorMessage);
      onError?.(errorMessage);
      setIsUploading(false);
      setFileName(null);
    }
  };

  const handleDragEnter = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(true);
  };

  const handleDragLeave = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(false);
  };

  const handleDragOver = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
  };

  const handleDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(false);

    const files = e.dataTransfer.files;
    if (files.length > 0) {
      handleUpload(files[0]);
    }
  };

  const handleFileInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.currentTarget.files;
    if (files && files.length > 0) {
      handleUpload(files[0]);
    }
  };

  const handleClickUpload = () => {
    fileInputRef.current?.click();
  };

  return (
    <div className="media-upload-container">
      <div
        className={`media-upload-zone ${isDragging ? 'dragging' : ''} ${
          isUploading ? 'uploading' : ''
        }`}
        onDragEnter={handleDragEnter}
        onDragLeave={handleDragLeave}
        onDragOver={handleDragOver}
        onDrop={handleDrop}
      >
        <input
          ref={fileInputRef}
          type="file"
          onChange={handleFileInputChange}
          style={{ display: 'none' }}
          accept={ALLOWED_MIME_TYPES.join(',')}
          disabled={isUploading}
        />

        {isUploading ? (
          <div className="upload-progress">
            <div className="progress-info">
              <p className="file-name">{fileName}</p>
              <p className="progress-percent">{Math.round(uploadProgress)}%</p>
            </div>
            <div className="progress-bar">
              <div
                className="progress-fill"
                style={{ width: `${uploadProgress}%` }}
              />
            </div>
          </div>
        ) : (
          <div className="upload-content">
            <svg
              className="upload-icon"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
            >
              <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
              <polyline points="17 8 12 3 7 8" />
              <line x1="12" y1="3" x2="12" y2="15" />
            </svg>
            <h3>Upload Media</h3>
            <p>Drag and drop your file here</p>
            <p className="or-text">or</p>
            <button
              type="button"
              className="btn btn-primary"
              onClick={handleClickUpload}
              disabled={isUploading}
            >
              Select File
            </button>
            <p className="file-types">
              Supported: Images (JPEG, PNG, GIF, WebP), Videos (MP4, WebM), PDFs
            </p>
            <p className="max-size">
              Max size: {(maxFileSize / 1024 / 1024).toFixed(0)}MB
            </p>
          </div>
        )}
      </div>

      {error && <div className="error-message">{error}</div>}
    </div>
  );
};

export default MediaUpload;
