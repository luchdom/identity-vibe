'use client';

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';

import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogContentText from '@mui/material/DialogContentText';
import DialogTitle from '@mui/material/DialogTitle';

import { authEvents } from '@/lib/auth/auth-events';
import OAuth2Auth, { type AuthenticatedUser } from '@/lib/auth/oauth2-auth';

export type AuthUser = AuthenticatedUser;

type AuthState = {
  user: AuthUser | null;
  scopes: string[];
  isAuthenticated: boolean;
  isLoading: boolean;
};

type AuthContextType = AuthState & {
  login: (user: AuthUser) => void;
  logout: () => Promise<void>;
  refreshAccessToken: () => Promise<boolean>;
  authorize: () => void;
};

const AuthContext = createContext<AuthContextType | undefined>(undefined);

type AuthProviderProps = {
  children: ReactNode;
};

export function AuthProvider({ children }: AuthProviderProps) {
  const [authState, setAuthState] = useState<AuthState>({
    user: null,
    scopes: [],
    isAuthenticated: false,
    isLoading: true,
  });
  const [showSessionExpired, setShowSessionExpired] = useState(false);

  const refreshAccessToken = useCallback(async (): Promise<boolean> => {
    try {
      const result = await OAuth2Auth.refreshToken();
      return Boolean(result.accessToken);
    } catch (error) {
      console.error('Failed to refresh token:', error);
      return false;
    }
  }, []);

  const initializeAuth = useCallback(async () => {
    try {
      const accessToken = OAuth2Auth.getAccessToken();
      const storedUser = OAuth2Auth.getUser();

      if (!accessToken || !storedUser) {
        setAuthState({
          user: null,
          scopes: [],
          isAuthenticated: false,
          isLoading: false,
        });
        return;
      }

      setAuthState({
        user: storedUser,
        scopes: OAuth2Auth.getScopes(),
        isAuthenticated: true,
        isLoading: false,
      });

      // E2E bypass avoids external auth-network dependency while still requiring local auth state.
      const shouldBypassAuthNetwork = process.env.NEXT_PUBLIC_E2E_BYPASS_AUTH === 'true' ||
        localStorage.getItem('__e2e_bypass_auth') === '1';
      if (shouldBypassAuthNetwork) {
        return;
      }

      try {
        const serverUser = await OAuth2Auth.getUserInfo();
        localStorage.setItem('user', JSON.stringify(serverUser));
        setAuthState({
          user: serverUser,
          scopes: OAuth2Auth.getScopes(),
          isAuthenticated: true,
          isLoading: false,
        });
      } catch {
        const refreshed = await refreshAccessToken();

        if (!refreshed) {
          setAuthState({
            user: null,
            scopes: [],
            isAuthenticated: false,
            isLoading: false,
          });
          return;
        }

        const refreshedUser = OAuth2Auth.getUser();
        setAuthState({
          user: refreshedUser,
          scopes: OAuth2Auth.getScopes(),
          isAuthenticated: Boolean(refreshedUser),
          isLoading: false,
        });
      }
    } catch {
      setAuthState({
        user: null,
        scopes: [],
        isAuthenticated: false,
        isLoading: false,
      });
    }
  }, [refreshAccessToken]);

  useEffect(() => {
    void initializeAuth();
  }, [initializeAuth]);

  useEffect(() => {
    const handleSessionExpired = () => {
      setShowSessionExpired(true);
    };

    authEvents.addEventListener('sessionExpired', handleSessionExpired);

    return () => {
      authEvents.removeEventListener('sessionExpired', handleSessionExpired);
    };
  }, []);

  const login = useCallback((user: AuthUser) => {
    setAuthState({
      user,
      scopes: OAuth2Auth.getScopes(),
      isAuthenticated: true,
      isLoading: false,
    });
  }, []);

  const authorize = useCallback(() => {
    OAuth2Auth.authorize().catch((error) => {
      console.error('Failed to start authorization flow:', error);
    });
  }, []);

  const logout = useCallback(async () => {
    setAuthState({
      user: null,
      scopes: [],
      isAuthenticated: false,
      isLoading: false,
    });

    await OAuth2Auth.logout();
  }, []);

  const contextValue = useMemo<AuthContextType>(() => ({
    ...authState,
    login,
    logout,
    refreshAccessToken,
    authorize,
  }), [authState, authorize, login, logout, refreshAccessToken]);

  return (
    <AuthContext.Provider value={contextValue}>
      {children}

      <Dialog
        open={showSessionExpired}
        onClose={(_, reason) => {
          if (reason !== 'backdropClick') {
            setShowSessionExpired(false);
          }
        }}
      >
        <DialogTitle>Session expired</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Your session is no longer valid. Sign in again to continue.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button
            variant="contained"
            onClick={() => {
              setShowSessionExpired(false);
              void logout();
            }}
          >
            Sign in again
          </Button>
        </DialogActions>
      </Dialog>
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }

  return context;
}
