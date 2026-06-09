import { apiClient } from './apiClient';

export type ExportFormat = 'json' | 'markdown';

/**
 * Download the current user's journal export in the given format.
 * Fetches as an authenticated blob (the endpoint requires a Bearer token, so a
 * plain anchor href won't work) and triggers a browser download.
 */
export const downloadExport = async (format: ExportFormat): Promise<void> => {
  const response = await apiClient.get(`/exports?format=${format}`, {
    responseType: 'blob',
  });

  // Prefer the server-provided filename; fall back to a sensible default.
  let filename = format === 'json' ? 'journal-export.json' : 'journal-export.md';
  const disposition = response.headers['content-disposition'] as string | undefined;
  const match = disposition?.match(/filename="?([^";]+)"?/);
  if (match) filename = match[1];

  const url = window.URL.createObjectURL(response.data as Blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  window.URL.revokeObjectURL(url);
};
