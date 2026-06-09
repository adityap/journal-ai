/**
 * Helpers for working with errors from axios/the API without resorting to `any`.
 * The shape mirrors what the backend returns: { response: { status, data: { message, errors } } }.
 */

export interface ApiError {
  response?: {
    status?: number;
    data?: {
      message?: string;
      errors?: Record<string, string[]>;
    };
  };
  message?: string;
}

/** Narrow an unknown caught value to the API error shape (empty object if not an object). */
export function asApiError(err: unknown): ApiError {
  return err && typeof err === 'object' ? (err as ApiError) : {};
}

/** Best-effort human-readable message from an unknown caught value. */
export function getErrorMessage(err: unknown, fallback = 'Something went wrong'): string {
  const e = asApiError(err);
  return e.response?.data?.message || e.message || fallback;
}
