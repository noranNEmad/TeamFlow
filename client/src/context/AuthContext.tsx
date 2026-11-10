import React, { createContext, useContext, useState } from 'react';
import { User, Organization, AuthResponse } from '../types';

interface AuthContextType {
  user: User | null;
  organization: Organization | null;
  token: string | null;
  isAuthenticated: boolean;
  login: (data: AuthResponse) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(() => {
    const saved = localStorage.getItem('teamflow_user');
    return saved ? JSON.parse(saved) : null;
  });

  const [organization, setOrganization] = useState<Organization | null>(() => {
    const saved = localStorage.getItem('teamflow_org');
    return saved ? JSON.parse(saved) : null;
  });

  const [token, setToken] = useState<string | null>(() => {
    return localStorage.getItem('teamflow_token');
  });

  const login = (data: AuthResponse) => {
    setUser(data.user);
    setOrganization(data.organization);
    setToken(data.token);

    localStorage.setItem('teamflow_token', data.token);
    localStorage.setItem('teamflow_user', JSON.stringify(data.user));
    localStorage.setItem('teamflow_org', JSON.stringify(data.organization));
  };

  const logout = () => {
    setUser(null);
    setOrganization(null);
    setToken(null);

    localStorage.removeItem('teamflow_token');
    localStorage.removeItem('teamflow_user');
    localStorage.removeItem('teamflow_org');
    window.location.href = '/login';
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        organization,
        token,
        isAuthenticated: !!token,
        login,
        logout,
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
