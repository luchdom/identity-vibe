'use client';

import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';

import CssBaseline from '@mui/material/CssBaseline';
import {
  ThemeProvider,
  type PaletteMode,
} from '@mui/material/styles';

import { createAppTheme } from './theme';

const STORAGE_KEY = 'web-app-color-mode';
const MEDIA_QUERY = '(prefers-color-scheme: dark)';

type ThemeModeContextValue = {
  mode: PaletteMode;
  setColorMode: (nextMode: PaletteMode) => void;
  toggleColorMode: () => void;
};

const ThemeModeContext = createContext<ThemeModeContextValue | undefined>(undefined);

type ThemeModeProviderProps = {
  children: ReactNode;
  fontFamily: string;
};

function isPaletteMode(value: string | null): value is PaletteMode {
  return value === 'light' || value === 'dark';
}

function getSystemMode(): PaletteMode {
  if (typeof window === 'undefined') {
    return 'light';
  }

  return window.matchMedia(MEDIA_QUERY).matches ? 'dark' : 'light';
}

export function ThemeModeProvider({ children, fontFamily }: ThemeModeProviderProps) {
  const [mode, setMode] = useState<PaletteMode>('light');
  const [followsSystem, setFollowsSystem] = useState(true);

  useEffect(() => {
    const storedMode = typeof window !== 'undefined'
      ? window.localStorage.getItem(STORAGE_KEY)
      : null;

    if (isPaletteMode(storedMode)) {
      setMode(storedMode);
      setFollowsSystem(false);
      return;
    }

    setMode(getSystemMode());
    setFollowsSystem(true);
  }, []);

  useEffect(() => {
    if (!followsSystem || typeof window === 'undefined') {
      return;
    }

    const mediaQuery = window.matchMedia(MEDIA_QUERY);

    const handleChange = (event: MediaQueryListEvent) => {
      setMode(event.matches ? 'dark' : 'light');
    };

    mediaQuery.addEventListener('change', handleChange);

    return () => {
      mediaQuery.removeEventListener('change', handleChange);
    };
  }, [followsSystem]);

  useEffect(() => {
    document.documentElement.style.colorScheme = mode;
  }, [mode]);

  const contextValue = useMemo<ThemeModeContextValue>(() => ({
    mode,
    setColorMode: (nextMode) => {
      setMode(nextMode);
      setFollowsSystem(false);
      window.localStorage.setItem(STORAGE_KEY, nextMode);
    },
    toggleColorMode: () => {
      setMode((currentMode) => {
        const nextMode = currentMode === 'light' ? 'dark' : 'light';
        window.localStorage.setItem(STORAGE_KEY, nextMode);
        return nextMode;
      });
      setFollowsSystem(false);
    },
  }), [mode]);

  const theme = useMemo(
    () => createAppTheme(mode, fontFamily),
    [fontFamily, mode]
  );

  return (
    <ThemeModeContext.Provider value={contextValue}>
      <ThemeProvider theme={theme}>
        <CssBaseline enableColorScheme />
        {children}
      </ThemeProvider>
    </ThemeModeContext.Provider>
  );
}

export function useThemeMode() {
  const context = useContext(ThemeModeContext);

  if (!context) {
    throw new Error('useThemeMode must be used within ThemeModeProvider');
  }

  return context;
}
