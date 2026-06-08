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

// Entry CRUD helpers live in entriesClient.ts (single source of truth);
// this module only owns the shared axios instance and response types.

export default apiClient;
