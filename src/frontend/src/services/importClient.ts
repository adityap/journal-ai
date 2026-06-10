import { apiClient } from './apiClient';

export interface ImportResult {
  imported: number;
  skippedDuplicates: number;
  failed: number;
  errors: string[];
}

/**
 * Import entries from a previously exported JSON file.
 *
 * Reads the file in the browser, parses it, and posts the entries to the import
 * endpoint. Accepts either the full export object ({ entries: [...] }) or a bare
 * array of entries. The server assigns ownership to the current user and skips
 * duplicates, so re-importing the same file is safe.
 */
export const importEntriesFromFile = async (file: File): Promise<ImportResult> => {
  const text = await file.text();

  let parsed: unknown;
  try {
    parsed = JSON.parse(text);
  } catch {
    throw new Error('That file is not valid JSON. Import expects a journal JSON export.');
  }

  // Normalize to { entries: [...] } regardless of whether the file is the full
  // export object or a bare array.
  let entries: unknown;
  if (Array.isArray(parsed)) {
    entries = parsed;
  } else if (parsed && typeof parsed === 'object' && 'entries' in parsed) {
    entries = (parsed as { entries: unknown }).entries;
  }

  if (!Array.isArray(entries)) {
    throw new Error('Could not find an "entries" array in that file.');
  }

  const response = await apiClient.post<ImportResult>('/imports', { entries });
  return response.data;
};
