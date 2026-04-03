import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

interface ProtectedRouteProps {
  element: React.ReactElement;
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ element }) => {
  const { isAuthenticated, loading } = useAuth();

  console.log('[ProtectedRoute] Check - isAuthenticated:', isAuthenticated, 'loading:', loading);

  if (loading) {
    console.log('[ProtectedRoute] Still loading...');
    return <div className="loading">Loading...</div>;
  }

  if (!isAuthenticated) {
    console.warn('[ProtectedRoute] Not authenticated, redirecting to /login');
  }

  return isAuthenticated ? element : <Navigate to="/login" replace />;
};
