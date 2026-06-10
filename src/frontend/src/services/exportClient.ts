import { apiClient } from './apiClient';

export type ExportFormat = 'json' | 'markdown';
export type ExportJobFormat = 'json' | 'markdown' | 'zip';

export interface ExportJob {
  id: string;
  format: ExportJobFormat;
  status: 'queued' | 'running' | 'completed' | 'failed';
  ready: boolean;
  error?: string;
  createdAt: string;
  completedAt?: string;
}

/** Trigger a browser download for a fetched blob, using a server-provided filename if present. */
const saveBlob = (data: Blob, headers: Record<string, unknown>, fallback: string): void => {
  let filename = fallback;
  const disposition = headers['content-disposition'] as string | undefined;
  const match = disposition?.match(/filename="?([^";]+)"?/);
  if (match) filename = match[1];

  const url = window.URL.createObjectURL(data);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  window.URL.revokeObjectURL(url);
};

/**
 * Download the current user's journal export in the given format.
 * Fetches as an authenticated blob (the endpoint requires a Bearer token, so a
 * plain anchor href won't work) and triggers a browser download.
 */
export const downloadExport = async (format: ExportFormat): Promise<void> => {
  const response = await apiClient.get(`/exports?format=${format}`, {
    responseType: 'blob',
  });
  saveBlob(response.data as Blob, response.headers, format === 'json' ? 'journal-export.json' : 'journal-export.md');
};

/** Start an async export job (e.g. a ZIP bundling entries + media). Returns the queued job. */
export const createExportJob = async (format: ExportJobFormat): Promise<ExportJob> => {
  const response = await apiClient.post<ExportJob>('/exports/jobs', { format });
  return response.data;
};

/** List the current user's recent export jobs. */
export const listExportJobs = async (): Promise<ExportJob[]> => {
  const response = await apiClient.get<ExportJob[]>('/exports/jobs');
  return response.data;
};

/** Download a completed export job's artifact. */
export const downloadExportJob = async (job: ExportJob): Promise<void> => {
  const response = await apiClient.get(`/exports/jobs/${job.id}/download`, {
    responseType: 'blob',
  });
  const ext = job.format === 'zip' ? 'zip' : job.format === 'json' ? 'json' : 'md';
  saveBlob(response.data as Blob, response.headers, `journal-export.${ext}`);
};
