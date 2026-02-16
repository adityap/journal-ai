import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import axios from 'axios';

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
    const storedToken = localStorage.getItem('authToken');
    const storedUser = localStorage.getItem('user');

    if (storedToken && storedUser) {
      setToken(storedToken);
      setUser(JSON.parse(storedUser));
      // Set default authorization header
      axios.defaults.headers.common['Authorization'] = `Bearer ${storedToken}`;
    }

    setLoading(false);
  }, []);

  const register = async (email: string, password: string): Promise<void> => {
    try {
      setError(null);
      setLoading(true);

      const response = await axios.post('/api/v1/auth/register', {
        email,
        password,
      });

      const { user: newUser, token: newToken } = response.data;

      setUser(newUser);
      setToken(newToken);

      // Persist to localStorage
      localStorage.setItem('authToken', newToken);
      localStorage.setItem('user', JSON.stringify(newUser));

      // Set default authorization header
      axios.defaults.headers.common['Authorization'] = `Bearer ${newToken}`;
    } catch (err) {
      const message = axios.isAxiosError(err) && err.response?.data?.message
        ? err.response.data.message
        : 'Registration failed. Please try again.';
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  };

  const login = async (email: string, password: string): Promise<void> => {
    try {
      setError(null);
      setLoading(true);

      const response = await axios.post('/api/v1/auth/login', {
        email,
        password,
      });

      const { user: loggedInUser, token: newToken } = response.data;

      setUser(loggedInUser);
      setToken(newToken);

      // Persist to localStorage
      localStorage.setItem('authToken', newToken);
      localStorage.setItem('user', JSON.stringify(loggedInUser));

      // Set default authorization header
      axios.defaults.headers.common['Authorization'] = `Bearer ${newToken}`;
    } catch (err) {
      const message = axios.isAxiosError(err) && err.response?.data?.message
        ? err.response.data.message
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
    localStorage.removeItem('authToken');
    localStorage.removeItem('user');

    // Remove authorization header
    delete axios.defaults.headers.common['Authorization'];
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
