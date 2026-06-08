import axios, { AxiosInstance, AxiosError } from 'axios';

/**
 * API Client Service
 * Handles all HTTP communication with the backend API
 * Includes JWT token management and error handling
 */

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api/v1';

// Create axios instance with default config
export const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30000,
});

// Request interceptor - add JWT token if available
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('auth_token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error: AxiosError) => {
    return Promise.reject(error);
  }
);

// Response interceptor - handle errors globally
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    // NOTE: NO redirects here. Redirects are handled by ProtectedRoute component.
    // The apiClient just rejects the promise so callers can handle the error.
    // This prevents page refreshes during login flow.
    return Promise.reject(error);
  }
);

/**
 * Note: Type imports moved to entriesClient.ts to maintain single source of truth
 */

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  perPage: number;
  totalPages: number;
}

export interface ErrorResponse {
  code?: string;
  message: string;
  errors?: Record<string, string[]>;
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

export default apiClient;
