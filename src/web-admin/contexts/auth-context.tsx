'use client';

import { createContext, useContext, useEffect, useState, useCallback, type ReactNode } from 'react';
import { authApi, type User } from '@/lib/auth-api';

export interface AuthState {
  user: User | null;
  isLoading: boolean;
  isAuthenticated: boolean;
}

export interface AuthContextType extends AuthState {
  login: (email: string, password: string) => Promise<{ success: boolean; error?: string }>;
  logout: () => Promise<void>;
  refreshAccessToken: () => Promise<boolean>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [authState, setAuthState] = useState<AuthState>({
    user: null,
    isLoading: true,
    isAuthenticated: false,
  });

  // Initialize auth state from stored data
  const initializeAuth = useCallback(async () => {
    try {
      const { accessToken } = authApi.getStoredTokens();
      const storedUser = authApi.getStoredUser();

      if (accessToken && storedUser) {
        try {
          // Verify token is still valid by fetching user profile
          const serverUser = await authApi.getProfile();
          
          setAuthState({
            user: serverUser,
            isAuthenticated: true,
            isLoading: false,
          });
          
          // Update stored user data if server returned updated info
          localStorage.setItem('user', JSON.stringify(serverUser));
        } catch (error) {
          // Token is invalid, clear storage
          authApi.clearAuthData();
          setAuthState({
            user: null,
            isAuthenticated: false,
            isLoading: false,
          });
        }
      } else {
        setAuthState({
          user: null,
          isAuthenticated: false,
          isLoading: false,
        });
      }
    } catch (error) {
      authApi.clearAuthData();
      setAuthState({
        user: null,
        isAuthenticated: false,
        isLoading: false,
      });
    }
  }, []);

  // Login function
  const login = useCallback(async (email: string, password: string): Promise<{ success: boolean; error?: string }> => {
    setAuthState(prev => ({ ...prev, isLoading: true }));
    
    try {
      const authData = await authApi.login({ email, password });
      
      // Store tokens and user data using AuthApi
      authApi.storeAuthData(authData);
      
      setAuthState({
        user: authData.user,
        isAuthenticated: true,
        isLoading: false,
      });

      return { success: true };
    } catch (error: any) {
      setAuthState(prev => ({ ...prev, isLoading: false }));
      return { 
        success: false, 
        error: error?.response?.data?.message || error?.message || 'Login failed' 
      };
    }
  }, []);

  // Logout function
  const logout = useCallback(async (): Promise<void> => {
    try {
      // Call logout endpoint using AuthApi
      await authApi.logout();
    } catch (error) {
      // Continue with logout even if API call fails
      console.warn('Logout API call failed:', error);
    }
    
    // Clear stored tokens and state using AuthApi
    authApi.clearAuthData();
    setAuthState({
      user: null,
      isAuthenticated: false,
      isLoading: false,
    });
  }, []);

  // Refresh access token
  const refreshAccessToken = useCallback(async (): Promise<boolean> => {
    const { refreshToken } = authApi.getStoredTokens();
    
    if (!refreshToken) {
      return false;
    }

    try {
      const response = await authApi.refreshTokens({ refreshToken });
      
      // Update tokens using AuthApi
      localStorage.setItem('accessToken', response.accessToken);
      if (response.refreshToken) {
        localStorage.setItem('refreshToken', response.refreshToken);
      }

      return true;
    } catch (error) {
      await logout();
      return false;
    }
  }, [logout]);

  // Initialize on mount
  useEffect(() => {
    initializeAuth();
  }, [initializeAuth]);

  const contextValue: AuthContextType = {
    ...authState,
    login,
    logout,
    refreshAccessToken,
  };

  return (
    <AuthContext.Provider value={contextValue}>
      {children}
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