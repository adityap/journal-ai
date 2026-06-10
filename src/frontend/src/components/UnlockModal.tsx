import React, { useState } from 'react';
import { unlock } from '../services/unlockClient';
import { getErrorMessage } from '../utils/errors';

interface UnlockModalProps {
  /** Called after a successful unlock so the caller can refresh its data. */
  onUnlocked: () => void;
  onClose: () => void;
}

/**
 * Prompts for the account password and starts an unlock session. The password is
 * entered in a masked field and sent only to POST /unlock; it is never stored.
 */
export const UnlockModal: React.FC<UnlockModalProps> = ({ onUnlocked, onClose }) => {
  const [password, setPassword] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!password) return;
    setSubmitting(true);
    setError(null);
    try {
      await unlock(password);
      onUnlocked();
    } catch (err) {
      setError(getErrorMessage(err, 'Incorrect password'));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-label="Unlock private entries"
      style={{
        position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.45)',
        display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000,
      }}
      onClick={onClose}
    >
      <form
        onSubmit={handleSubmit}
        onClick={(e) => e.stopPropagation()}
        style={{
          background: '#fff', padding: '1.5rem', borderRadius: '8px',
          width: 'min(92vw, 380px)', boxShadow: '0 10px 30px rgba(0,0,0,0.2)',
        }}
      >
        <h2 style={{ margin: '0 0 0.5rem', fontSize: '1.15rem' }}>🔒 Unlock private entries</h2>
        <p style={{ margin: '0 0 1rem', color: '#555', fontSize: '0.9rem' }}>
          Enter your account password. Private entries stay unlocked for one hour or until you log out.
        </p>
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          placeholder="Account password"
          aria-label="Account password"
          autoFocus
          style={{
            width: '100%', padding: '0.6rem 0.75rem', boxSizing: 'border-box',
            border: '1px solid #ddd', borderRadius: '6px', fontSize: '1rem',
          }}
        />
        {error && (
          <div role="alert" style={{ color: '#c62828', marginTop: '0.6rem', fontSize: '0.9rem' }}>
            {error}
          </div>
        )}
        <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'flex-end', marginTop: '1.2rem' }}>
          <button type="button" className="btn btn-secondary btn-sm" onClick={onClose} disabled={submitting}>
            Cancel
          </button>
          <button type="submit" className="btn btn-primary btn-sm" disabled={submitting || !password}>
            {submitting ? 'Unlocking…' : 'Unlock'}
          </button>
        </div>
      </form>
    </div>
  );
};
