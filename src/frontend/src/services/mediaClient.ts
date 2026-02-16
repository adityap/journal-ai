import { apiClient } from './apiClient';

/**
 * Media API Client
 * Handles all media-related API calls including upload, retrieval, and management
 */

export interface MediaInitiateResponse {
  mediaId: string;
  uploadId: string;
  presignedUrl: string;
  expiresIn: number;
}

export interface MediaCompleteRequest {
  mediaId: string;
  uploadId: string;
}

export interface MediaFile {
  id: string;
  userId: string;
  fileName: string;
  mimeType: string;
  size: number;
  storageKey: string;
  presignedDownloadUrl?: string;
  thumbnailUrl?: string;
  associatedEntryId?: string;
  createdAt: string;
  updatedAt: string;
}

export interface MediaPagedResult {
  items: MediaFile[];
  totalCount: number;
  page: number;
  perPage: number;
  totalPages: number;
}

/**
 * Initiate a media upload by requesting a presigned URL
 */
export const initiateMediaUpload = async (
  fileName: string,
  mimeType: string,
  size: number
): Promise<MediaInitiateResponse> => {
  const response = await apiClient.post<MediaInitiateResponse>('/media/initiate', {
    fileName,
    mimeType,
    size,
  });
  return response.data;
};

/**
 * Complete a media upload after file has been uploaded to S3
 */
export const completeMediaUpload = async (
  mediaId: string,
  uploadId: string
): Promise<MediaFile> => {
  const response = await apiClient.post<MediaFile>('/media/complete', {
    mediaId,
    uploadId,
  });
  return response.data;
};

/**
 * Get a specific media file metadata
 */
export const getMediaFile = async (mediaId: string): Promise<MediaFile> => {
  const response = await apiClient.get<MediaFile>(`/media/${mediaId}`);
  return response.data;
};

/**
 * List all media files for the current user with pagination
 */
export const listMediaFiles = async (
  page: number = 1,
  perPage: number = 20,
  entryId?: string
): Promise<MediaPagedResult> => {
  const params = new URLSearchParams({
    page: page.toString(),
    perPage: perPage.toString(),
  });

  if (entryId) {
    params.append('entryId', entryId);
  }

  const response = await apiClient.get<MediaPagedResult>(`/media?${params}`);
  return response.data;
};

/**
 * Delete a media file
 */
export const deleteMediaFile = async (mediaId: string): Promise<void> => {
  await apiClient.delete(`/media/${mediaId}`);
};

/**
 * Associate a media file with a journal entry
 */
export const associateMediaWithEntry = async (
  mediaId: string,
  entryId: string
): Promise<MediaFile> => {
  const response = await apiClient.post<MediaFile>(
    `/media/${mediaId}/associate-entry/${entryId}`,
    {}
  );
  return response.data;
};

/**
 * Upload a file directly to S3 using presigned URL
 */
export const uploadFileToS3 = async (
  presignedUrl: string,
  file: File,
  onProgress?: (progress: number) => void
): Promise<void> => {
  const xhr = new XMLHttpRequest();

  return new Promise((resolve, reject) => {
    xhr.upload.addEventListener('progress', (event) => {
      if (event.lengthComputable && onProgress) {
        const percentComplete = (event.loaded / event.total) * 100;
        onProgress(percentComplete);
      }
    });

    xhr.addEventListener('load', () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        resolve();
      } else {
        reject(new Error(`Upload failed with status ${xhr.status}`));
      }
    });

    xhr.addEventListener('error', () => {
      reject(new Error('Upload failed'));
    });

    xhr.addEventListener('abort', () => {
      reject(new Error('Upload aborted'));
    });

    xhr.open('PUT', presignedUrl);
    xhr.setRequestHeader('Content-Type', file.type);
    xhr.send(file);
  });
};

export default {
  initiateMediaUpload,
  completeMediaUpload,
  getMediaFile,
  listMediaFiles,
  deleteMediaFile,
  associateMediaWithEntry,
  uploadFileToS3,
};
