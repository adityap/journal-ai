import { apiClient } from './apiClient';

/**
 * Entries API Client
 * Handles all entry-related API calls with proper typing and error handling
 */

export interface Entry {
  id: string;
  title?: string;
  bodyText?: string;
  type: string;
  confidentiality: string;
  categoryId?: string;
  tags?: string[];
  sentimentScore?: number;
  sentimentLabel?: string;
  createdAt: string;
  readOnlyAfter: string;
  immutable: boolean;
}

export interface CreateEntryRequest {
  title?: string;
  bodyText?: string;
  type?: string;
  confidentiality: string;
  categoryId?: string;
  tags?: string[];
  sentimentScore?: number;
  sentimentLabel?: string;
  sentimentModel?: string;
}

export interface UpdateEntryRequest {
  title?: string;
  bodyText?: string;
  categoryId?: string;
  tags?: string[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  perPage: number;
  totalPages: number;
}

/**
 * List entries with pagination and filtering
 */
export const listEntries = async (
  page: number = 1,
  perPage: number = 20,
  filters?: {
    start?: Date;
    end?: Date;
    categoryId?: string;
    tag?: string;
  }
): Promise<PagedResult<Entry>> => {
  const params = new URLSearchParams({
    page: page.toString(),
    perPage: perPage.toString(),
  });

  if (filters?.start) params.append('start', filters.start.toISOString());
  if (filters?.end) params.append('end', filters.end.toISOString());
  if (filters?.categoryId) params.append('categoryId', filters.categoryId);
  if (filters?.tag) params.append('tag', filters.tag);

  const response = await apiClient.get<PagedResult<Entry>>(`/entries?${params}`);
  return response.data;
};

/**
 * Get a single entry by ID
 */
export const getEntry = async (id: string): Promise<Entry> => {
  const response = await apiClient.get<Entry>(`/entries/${id}`);
  return response.data;
};

/**
 * Create a new entry
 */
export const createEntry = async (data: CreateEntryRequest): Promise<Entry> => {
  const response = await apiClient.post<Entry>('/entries', data);
  return response.data;
};

/**
 * Update an existing entry
 */
export const updateEntry = async (id: string, data: UpdateEntryRequest): Promise<Entry> => {
  const response = await apiClient.patch<Entry>(`/entries/${id}`, data);
  return response.data;
};

/**
 * Delete an entry
 */
export const deleteEntry = async (id: string): Promise<void> => {
  await apiClient.delete(`/entries/${id}`);
};

/**
 * Check if an entry is immutable (past the read-only deadline)
 * Uses the 'immutable' flag from the backend which is more reliable
 */
export const isEntryImmutable = (entry: Entry): boolean => {
  // If the entry has an explicit immutable flag, use it
  if (entry.immutable !== undefined) {
    return entry.immutable;
  }
  // Fallback: calculate based on readOnlyAfter if immutable flag is missing
  const nowUtc = new Date();
  const readOnlyAfterUtc = new Date(entry.readOnlyAfter);
  return nowUtc > readOnlyAfterUtc;
};

/**
 * Format entry date for display
 */
export const formatEntryDate = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
};

/**
 * Get confidentiality badge color
 */
export const getConfidentialityColor = (
  confidentiality: string
): { bgColor: string; textColor: string } => {
  switch (confidentiality.toLowerCase()) {
    case 'private':
      return { bgColor: '#ffebee', textColor: '#c62828' };
    case 'public':
      return { bgColor: '#e8f5e9', textColor: '#2e7d32' };
    default:
      return { bgColor: '#f5f5f5', textColor: '#333' };
  }
};

// Named export for compatibility
export const entriesClient = {
  listEntries,
  getEntry,
  createEntry,
  updateEntry,
  deleteEntry,
  isEntryImmutable,
  formatEntryDate,
  getConfidentialityColor,
};

export default entriesClient;
