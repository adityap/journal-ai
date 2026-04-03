// @refresh reset
import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { apiClient } from '../services/apiClient';

interface User {
  id: string;
  email: string;
}

interface AuthContextType {
  user: User | null;
  token: string | null;
  loading: boolean;
  error: string | null;
  register: (email: string, password: string) => Promise<void>;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Initialize from localStorage on mount
  useEffect(() => {
    console.log('[AuthContext] Initializing...');
    const storedToken = localStorage.getItem('auth_token');
    const storedUser = localStorage.getItem('auth_user');

    console.log('[AuthContext] Found token in storage:', !!storedToken);
    console.log('[AuthContext] Found user in storage:', !!storedUser);

    if (storedToken && storedUser) {
      setToken(storedToken);
      setUser(JSON.parse(storedUser));
      // Set default authorization header
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${storedToken}`;
      console.log('[AuthContext] Set Authorization header from stored token');
    }

    setLoading(false);
  }, []);

  const register = async (email: string, password: string): Promise<void> => {
    try {
      setError(null);
      setLoading(true);

      const response = await apiClient.post('/auth/register', {
        email,
        password,
      });

      const { user: newUser, accessToken: newToken } = response.data;

      setUser(newUser);
      setToken(newToken);

      // Persist to localStorage
      localStorage.setItem('auth_token', newToken);
      localStorage.setItem('auth_user', JSON.stringify(newUser));

      // Set default authorization header
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${newToken}`;
    } catch (err) {
      const message = err instanceof Error && (err as any).response?.data?.message
        ? (err as any).response.data.message
        : 'Registration failed. Please try again.';
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  };

  const login = async (email: string, password: string): Promise<void> => {
    try {
      console.log('[AuthContext] Login attempt for:', email);
      setError(null);
      setLoading(true);

      console.log('[AuthContext] Calling API POST /auth/login...');
      const response = await apiClient.post('/auth/login', {
        email,
        password,
      });

      console.log('[AuthContext] Login response received:', response.status);
      console.log('[AuthContext] Full response data:', response.data);
      console.log('[AuthContext] Response data keys:', Object.keys(response.data));

      const { user: loggedInUser, accessToken: newToken } = response.data;

      console.log('[AuthContext] Extracted token:', newToken ? `${newToken.substring(0, 20)}...` : 'NO TOKEN');
      console.log('[AuthContext] Extracted user:', loggedInUser);

      if (!newToken) {
        console.error('[AuthContext] ERROR: newToken is null/undefined!');
        throw new Error('No token in response');
      }

      if (!loggedInUser) {
        console.error('[AuthContext] ERROR: loggedInUser is null/undefined!');
        throw new Error('No user in response');
      }

      console.log('[AuthContext] Setting user state...');
      setUser(loggedInUser);
      
      console.log('[AuthContext] Setting token state...');
      setToken(newToken);

      console.log('[AuthContext] Saving token to localStorage...');
      localStorage.setItem('auth_token', newToken);
      console.log('[AuthContext] Successfully saved auth_token');

      console.log('[AuthContext] Saving user to localStorage...');
      localStorage.setItem('auth_user', JSON.stringify(loggedInUser));
      console.log('[AuthContext] Successfully saved auth_user');

      console.log('[AuthContext] Verifying localStorage...');
      const verify_token = localStorage.getItem('auth_token');
      const verify_user = localStorage.getItem('auth_user');
      console.log('[AuthContext] Verify token in storage:', !!verify_token);
      console.log('[AuthContext] Verify user in storage:', !!verify_user);

      console.log('[AuthContext] Setting Authorization header...');
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${newToken}`;
      console.log('[AuthContext] Authorization header set');
      
      console.log('[AuthContext] Login complete - isAuthenticated should be: true');
    } catch (err) {
      console.error('[AuthContext] LOGIN ERROR:', err);
      console.error('[AuthContext] Error type:', err instanceof Error ? err.message : String(err));
      if ((err as any).response) {
        console.error('[AuthContext] API Response status:', (err as any).response.status);
        console.error('[AuthContext] API Response data:', (err as any).response.data);
      }
      
      const message = err instanceof Error && (err as any).response?.data?.message
        ? (err as any).response.data.message
        : 'Login failed. Please check your credentials.';
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  };

  const logout = (): void => {
    setUser(null);
    setToken(null);
    setError(null);

    // Clear localStorage
    localStorage.removeItem('auth_token');
    localStorage.removeItem('auth_user');

    // Remove authorization header
    delete apiClient.defaults.headers.common['Authorization'];
  };

  const value: AuthContextType = {
    user,
    token,
    loading,
    error,
    register,
    login,
    logout,
    isAuthenticated: !!token && !!user,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
