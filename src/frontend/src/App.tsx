import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import { ProtectedRoute } from './components/ProtectedRoute';
import { LoginForm } from './components/LoginForm';
import { RegisterForm } from './components/RegisterForm';
import { Timeline } from './components/Timeline';
import { EntryForm } from './components/EntryForm';
import { EntryDetail } from './components/EntryDetail';
import './App.css';
import './styles/media.css';

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          {/* Public routes */}
          <Route path="/login" element={<LoginForm />} />
          <Route path="/register" element={<RegisterForm />} />

          {/* Protected routes */}
          <Route
            path="/"
            element={<ProtectedRoute element={<Timeline />} />}
          />
          <Route
            path="/entries/new"
            element={<ProtectedRoute element={<EntryForm />} />}
          />
          <Route
            path="/entries/:id/edit"
            element={<ProtectedRoute element={<EntryForm />} />}
          />
          <Route
            path="/entries/:id"
            element={<ProtectedRoute element={<EntryDetail />} />}
          />

          {/* Redirect unknown routes to home */}
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
