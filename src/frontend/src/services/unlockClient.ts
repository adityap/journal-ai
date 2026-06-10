import { apiClient } from './apiClient';

/**
 * Unlock client: trades the account password for a short-lived session token that
 * reveals the user's private entries. The token lives in sessionStorage (cleared when
 * the tab closes) and is attached to requests by the apiClient as the X-Unlock-Token
 * header. The server still enforces its own 1-hour expiry and revokes on logout.
 */

const TOKEN_KEY = 'unlock_token';
const EXPIRES_KEY = 'unlock_expires_at';

interface UnlockResponse {
  token: string;
  expiresAt: string;
}

/** Verify the account password and start an unlock session. Returns the expiry. */
export const unlock = async (password: string): Promise<Date> => {
  const response = await apiClient.post<UnlockResponse>('/unlock', { password });
  const { token, expiresAt } = response.data;
  sessionStorage.setItem(TOKEN_KEY, token);
  sessionStorage.setItem(EXPIRES_KEY, expiresAt);
  return new Date(expiresAt);
};

/** The current unlock token if the session is still valid, else null (and cleared). */
export const getUnlockToken = (): string | null => {
  const token = sessionStorage.getItem(TOKEN_KEY);
  const expiresAt = sessionStorage.getItem(EXPIRES_KEY);
  if (!token || !expiresAt) return null;

  if (new Date(expiresAt).getTime() <= Date.now()) {
    clearUnlock();
    return null;
  }
  return token;
};

/** True when there is a live unlock session. */
export const isUnlocked = (): boolean => getUnlockToken() !== null;

/** Drop the unlock session locally (e.g. on logout). */
export const clearUnlock = (): void => {
  sessionStorage.removeItem(TOKEN_KEY);
  sessionStorage.removeItem(EXPIRES_KEY);
};
