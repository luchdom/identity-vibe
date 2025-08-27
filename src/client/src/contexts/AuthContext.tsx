import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { api } from '@/lib/api';

export interface User {
  id: string;
  email: string;
  name?: string;
  roles?: string[];
}

export interface AuthState {
  user: User | null;
  accessToken: string | null;
  refreshToken: string | null;
  isLoading: boolean;
  isAuthenticated: boolean;
}

export interface AuthContextType extends AuthState {
  login: (email: string, password: string) => Promise<{ success: boolean; error?: string }>;
  register: (email: string, password: string, confirmPassword: string) => Promise<void>;
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
    accessToken: null,
    refreshToken: null,
    isLoading: true,
    isAuthenticated: false,
  });

  useEffect(() => {
    initializeAuth();
  }, []);

  const initializeAuth = async () => {
    try {
      const storedAccessToken = localStorage.getItem('accessToken');
      const storedRefreshToken = localStorage.getItem('refreshToken');
      const storedUser = localStorage.getItem('user');

      if (storedAccessToken && storedUser) {
        try {
          // Verify token is still valid by making a request to user info
          const response = await api.get('/auth/user');
          const serverUser = response.data;
          
          setAuthState({
            user: serverUser,
            accessToken: storedAccessToken,
            refreshToken: storedRefreshToken || null,
            isAuthenticated: true,
            isLoading: false,
          });
          
          // Update stored user data if server returned updated info
          localStorage.setItem('user', JSON.stringify(serverUser));
        } catch (error) {
          // Token is invalid, clear storage
          clearAuthData();
          setAuthState(prev => ({ ...prev, isLoading: false }));
        }
      } else {
        setAuthState(prev => ({ ...prev, isLoading: false }));
      }
    } catch (error) {
      clearAuthData();
      setAuthState(prev => ({ ...prev, isLoading: false }));
    }
  };

  const clearAuthData = () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
  };

  const login = async (email: string, password: string): Promise<{ success: boolean; error?: string }> => {
    setAuthState(prev => ({ ...prev, isLoading: true }));
    
    try {
      const response = await api.post('/auth/login', { 
        email, 
        password 
      });

      const { access_token, refresh_token } = response.data;
      
      // Get user info after successful login
      const userResponse = await api.get('/auth/user', {
        headers: { Authorization: `Bearer ${access_token}` }
      });
      
      const user = userResponse.data;
      
      // Store tokens and user data
      localStorage.setItem('accessToken', access_token);
      if (refresh_token) {
        localStorage.setItem('refreshToken', refresh_token);
      }
      localStorage.setItem('user', JSON.stringify(user));

      setAuthState({
        user,
        accessToken: access_token,
        refreshToken: refresh_token || null,
        isLoading: false,
        isAuthenticated: true,
      });

      return { success: true };
    } catch (error: any) {
      setAuthState(prev => ({ ...prev, isLoading: false }));
      return { 
        success: false, 
        error: error?.response?.data?.message || error?.message || 'Login failed' 
      };
    }
  };

  const register = async (email: string, password: string, confirmPassword: string): Promise<void> => {
    setAuthState(prev => ({ ...prev, isLoading: true }));
    
    try {
      await api.post('/account/register', { 
        email, 
        password, 
        confirmPassword 
      });

      // After successful registration, automatically log in
      await login(email, password);
    } catch (error) {
      setAuthState(prev => ({ ...prev, isLoading: false }));
      throw error;
    }
  };

  const logout = async (): Promise<void> => {
    try {
      // Call logout endpoint
      await api.post('/auth/logout');
    } catch (error) {
      // Continue with logout even if API call fails
      console.warn('Logout API call failed:', error);
    }
    
    // Clear stored tokens and state
    clearAuthData();
    setAuthState({
      user: null,
      accessToken: null,
      refreshToken: null,
      isLoading: false,
      isAuthenticated: false,
    });
  };

  const refreshAccessToken = async (): Promise<boolean> => {
    const { refreshToken } = authState;
    
    if (!refreshToken) {
      return false;
    }

    try {
      const response = await api.post('/auth/refresh', { 
        refreshToken 
      });
      
      const { accessToken: newAccessToken, refreshToken: newRefreshToken } = response.data;
      
      // Update tokens
      localStorage.setItem('accessToken', newAccessToken);
      if (newRefreshToken) {
        localStorage.setItem('refreshToken', newRefreshToken);
      }

      setAuthState(prev => ({
        ...prev,
        accessToken: newAccessToken,
        refreshToken: newRefreshToken || prev.refreshToken,
      }));

      return true;
    } catch (error) {
      await logout();
      return false;
    }
  };

  const contextValue: AuthContextType = {
    ...authState,
    login,
    register,
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