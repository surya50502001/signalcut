import React, { createContext, useContext, useState, useEffect } from 'react';
import apiClient from '../api/client';

export interface User {
  id: string;
  email: string;
  fullName: string;
  role: string;
  isEmailVerified: boolean;
  hasUsedFreeTrial: boolean;
}

export interface Organization {
  id: string;
  name: string;
  slug: string;
  availableCredits: number;
}

interface AuthContextType {
  user: User | null;
  organization: Organization | null;
  token: string | null;
  isAuthenticated: boolean;
  login: (email: string, pass: string) => Promise<void>;
  register: (email: string, pass: string, name: string, orgName?: string) => Promise<void>;
  logout: () => void;
  refreshWallet: () => Promise<void>;
  loginDemo: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [token, setToken] = useState<string | null>(() => {
    const saved = localStorage.getItem('signalcut_token');
    if (saved === 'mock_demo_jwt_token') {
      localStorage.removeItem('signalcut_token');
      localStorage.removeItem('signalcut_user');
      localStorage.removeItem('signalcut_org');
      return null;
    }
    return saved;
  });
  const [user, setUser] = useState<User | null>(() => {
    const saved = localStorage.getItem('signalcut_user');
    return saved ? JSON.parse(saved) : null;
  });
  const [organization, setOrganization] = useState<Organization | null>(() => {
    const saved = localStorage.getItem('signalcut_org');
    return saved ? JSON.parse(saved) : null;
  });

  const saveAuth = (newToken: string, newUser: User, newOrg: Organization) => {
    setToken(newToken);
    setUser(newUser);
    setOrganization(newOrg);
    localStorage.setItem('signalcut_token', newToken);
    localStorage.setItem('signalcut_user', JSON.stringify(newUser));
    localStorage.setItem('signalcut_org', JSON.stringify(newOrg));
  };

  const login = async (email: string, pass: string) => {
    const res = await apiClient.post('/api/v1/auth/login', { email, password: pass });
    const { token: tok, user: u, organization: org } = res.data.data;
    saveAuth(tok, u, org);
  };

  const register = async (email: string, pass: string, name: string, orgName?: string) => {
    const res = await apiClient.post('/api/v1/auth/register', {
      email,
      password: pass,
      fullName: name,
      organizationName: orgName
    });
    const { token: tok, user: u, organization: org } = res.data.data;
    saveAuth(tok, u, org);
  };

  const loginDemo = async () => {
    await login('demo@signalcut.app', 'DemoPassword123!');
  };

  const logout = () => {
    setToken(null);
    setUser(null);
    setOrganization(null);
    localStorage.removeItem('signalcut_token');
    localStorage.removeItem('signalcut_user');
    localStorage.removeItem('signalcut_org');
  };

  const refreshWallet = async () => {
    try {
      const res = await apiClient.get('/api/v1/credits/wallet');
      if (res.data?.data) {
        setOrganization((prev) =>
          prev ? { ...prev, availableCredits: res.data.data.availableBalance } : prev
        );
      }
    } catch {
      // ignore
    }
  };

  useEffect(() => {
    if (token) {
      refreshWallet();
    }
  }, [token]);

  return (
    <AuthContext.Provider
      value={{
        user,
        organization,
        token,
        isAuthenticated: !!token,
        login,
        register,
        logout,
        refreshWallet,
        loginDemo,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
