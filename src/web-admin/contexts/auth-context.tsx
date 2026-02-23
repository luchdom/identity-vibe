'use client';

import { createContext, useContext, useEffect, useState, useCallback, type ReactNode } from 'react';
import { useRouter } from 'next/navigation';
import OAuth2Auth from '@/lib/oauth2-auth';
import { SessionExpiredModal } from '@/components/auth/session-expired-modal';
import { authEvents } from '@/lib/api';

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  isActive: boolean;
  createdAt: string;
}

export interface AuthState {
  user: User | null;
  isLoading: boolean;
  isAuthenticated: boolean;
}

export interface AuthContextType extends AuthState {
  login: (userOrEmail: User | string, passwordOrToken?: string) => Promise<{ success: boolean; error?: string }> | void;
  logout: () => Promise<void>;
  refreshAccessToken: () => Promise<boolean>;
  authorize: () => void;
  handleSessionExpired: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const router = useRouter();
  const [authState, setAuthState] = useState<AuthState>({
    user: null,
    isLoading: true,
    isAuthenticated: false,
  });
  const [showSessionExpired, setShowSessionExpired] = useState(false);

  // Refresh access token
  const refreshAccessToken = useCallback(async (): Promise<boolean> => {
    try {
      const result = await OAuth2Auth.refreshToken();
      return !!result.accessToken;
    } catch (error) {
      console.error('Failed to refresh token:', error);
      return false;
    }
  }, []);

  // Initialize auth state from stored data
  const initializeAuth = useCallback(async () => {
    try {
      const accessToken = OAuth2Auth.getAccessToken();
      const storedUser = OAuth2Auth.getUser();

      if (accessToken && storedUser) {
        setAuthState({
          user: storedUser,
          isAuthenticated: true,
          isLoading: false,
        });

        // Optionally verify token is still valid
        try {
          const serverUser = await OAuth2Auth.getUserInfo();
          setAuthState({
            user: serverUser,
            isAuthenticated: true,
            isLoading: false,
          });
          localStorage.setItem('user', JSON.stringify(serverUser));
        } catch (error) {
          // Token might be expired, try to refresh
          const refreshed = await refreshAccessToken();
          if (!refreshed) {
            OAuth2Auth.logout();
            setAuthState({
              user: null,
              isAuthenticated: false,
              isLoading: false,
            });
          }
        }
      } else {
        setAuthState({
          user: null,
          isAuthenticated: false,
          isLoading: false,
        });
      }
    } catch (error) {
      setAuthState({
        user: null,
        isAuthenticated: false,
        isLoading: false,
      });
    }
  }, [refreshAccessToken]);

  // Login function - supports both OAuth2 callback and direct login
  const login = useCallback((userOrEmail: User | string, passwordOrToken?: string): Promise<{ success: boolean; error?: string }> | void => {
    // OAuth2 callback login (user object and token provided)
    if (typeof userOrEmail === 'object' && passwordOrToken) {
      setAuthState({
        user: userOrEmail,
        isAuthenticated: true,
        isLoading: false,
      });
      return;
    }

    // Direct password login (deprecated - will be removed)
    if (typeof userOrEmail === 'string' && passwordOrToken) {
      // For now, just redirect to OAuth2 flow
      authorize();
      return Promise.resolve({ success: false, error: 'Please use the OAuth2 login flow' });
    }

    return Promise.resolve({ success: false, error: 'Invalid login parameters' });
  }, []);

  // Initiate OAuth2 authorization flow
  const authorize = useCallback(() => {
    // Save current URL for redirect after auth
    if (window.location.pathname !== '/login' && window.location.pathname !== '/callback') {
      sessionStorage.setItem('returnUrl', window.location.pathname);
    }
    OAuth2Auth.authorize();
  }, []);

  // Handle session expired
  const handleSessionExpired = useCallback(() => {
    setShowSessionExpired(true);
  }, []);

  // Handle session expired confirmation
  const handleSessionExpiredConfirm = useCallback(() => {
    setShowSessionExpired(false);
    setAuthState({
      user: null,
      isAuthenticated: false,
      isLoading: false,
    });
    OAuth2Auth.logout();
  }, []);

  // Logout function
  const logout = useCallback(async (): Promise<void> => {
    setAuthState({
      user: null,
      isAuthenticated: false,
      isLoading: false,
    });

    // Use OAuth2 logout which handles everything
    await OAuth2Auth.logout();
  }, []);

  // Initialize on mount
  useEffect(() => {
    initializeAuth();
  }, [initializeAuth]);

  // Listen for session expired events from API interceptor
  useEffect(() => {
    const handleSessionExpiredEvent = () => {
      handleSessionExpired();
    };

    authEvents.addEventListener('sessionExpired', handleSessionExpiredEvent);

    return () => {
      authEvents.removeEventListener('sessionExpired', handleSessionExpiredEvent);
    };
  }, [handleSessionExpired]);

  const contextValue: AuthContextType = {
    ...authState,
    login,
    logout,
    refreshAccessToken,
    authorize,
    handleSessionExpired,
  };

  return (
    <AuthContext.Provider value={contextValue}>
      {children}
      <SessionExpiredModal
        open={showSessionExpired}
        onConfirm={handleSessionExpiredConfirm}
      />
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};