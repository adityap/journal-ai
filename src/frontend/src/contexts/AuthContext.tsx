// @refresh reset
import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { apiClient } from '../services/apiClient';
import { getErrorMessage } from '../utils/errors';

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
    const storedToken = localStorage.getItem('auth_token');
    const storedUser = localStorage.getItem('auth_user');

    if (storedToken && storedUser) {
      setToken(storedToken);
      setUser(JSON.parse(storedUser));
      // Set default authorization header
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${storedToken}`;
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
      setError(getErrorMessage(err, 'Registration failed. Please try again.'));
      throw err;
    } finally {
      setLoading(false);
    }
  };

  const login = async (email: string, password: string): Promise<void> => {
    try {
      setError(null);
      setLoading(true);

      const response = await apiClient.post('/auth/login', {
        email,
        password,
      });

      const { user: loggedInUser, accessToken: newToken } = response.data;

      if (!newToken) {
        throw new Error('No token in response');
      }

      if (!loggedInUser) {
        throw new Error('No user in response');
      }

      setUser(loggedInUser);
      setToken(newToken);

      localStorage.setItem('auth_token', newToken);
      localStorage.setItem('auth_user', JSON.stringify(loggedInUser));

      apiClient.defaults.headers.common['Authorization'] = `Bearer ${newToken}`;
    } catch (err) {
      setError(getErrorMessage(err, 'Login failed. Please check your credentials.'));
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

// Colocated with the provider by design; the hook + provider belong together.
// eslint-disable-next-line react-refresh/only-export-components
export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
