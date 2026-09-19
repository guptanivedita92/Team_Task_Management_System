import { createContext, useContext, useEffect, useState } from 'react';
import * as authService from '../services/authService';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => {
    const stored = localStorage.getItem('ttms_user');
    return stored ? JSON.parse(stored) : null;
  });
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (user) {
      localStorage.setItem('ttms_user', JSON.stringify(user));
    } else {
      localStorage.removeItem('ttms_user');
    }
  }, [user]);

  async function login(email, password) {
    setLoading(true);
    try {
      const result = await authService.login(email, password);
      localStorage.setItem('ttms_token', result.token);
      setUser(result.user);
      return result.user;
    } finally {
      setLoading(false);
    }
  }

  async function register(payload) {
    setLoading(true);
    try {
      const result = await authService.register(payload);
      localStorage.setItem('ttms_token', result.token);
      setUser(result.user);
      return result.user;
    } finally {
      setLoading(false);
    }
  }

  function logout() {
    localStorage.removeItem('ttms_token');
    localStorage.removeItem('ttms_user');
    setUser(null);
  }

  const value = { user, setUser, loading, login, register, logout, isAuthenticated: !!user };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider');
  return ctx;
}
