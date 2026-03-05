'use client';

import {
  Suspense,
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import { usePathname, useSearchParams } from 'next/navigation';

type NavigationProgressContextValue = {
  isNavigating: boolean;
  startNavigation: () => void;
};

const NavigationProgressContext = createContext<NavigationProgressContextValue | undefined>(undefined);
const defaultContextValue: NavigationProgressContextValue = {
  isNavigating: false,
  startNavigation: () => {},
};

type NavigationProgressProviderProps = {
  children: ReactNode;
};

function NavigationProgressProviderInner({ children }: NavigationProgressProviderProps) {
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [isNavigating, setIsNavigating] = useState(false);

  const routeKey = `${pathname}?${searchParams.toString()}`;

  const clearFallbackTimeout = useCallback(() => {
    if (!timeoutRef.current) {
      return;
    }

    clearTimeout(timeoutRef.current);
    timeoutRef.current = null;
  }, []);

  const startNavigation = useCallback(() => {
    setIsNavigating(true);
    clearFallbackTimeout();

    // Safety fallback in case navigation is interrupted and route key does not change.
    timeoutRef.current = setTimeout(() => {
      setIsNavigating(false);
      timeoutRef.current = null;
    }, 10000);
  }, [clearFallbackTimeout]);

  useEffect(() => {
    if (!isNavigating) {
      return;
    }

    clearFallbackTimeout();

    const settleTimeout = setTimeout(() => {
      setIsNavigating(false);
    }, 160);

    return () => {
      clearTimeout(settleTimeout);
    };
  }, [clearFallbackTimeout, isNavigating, routeKey]);

  useEffect(() => () => {
    clearFallbackTimeout();
  }, [clearFallbackTimeout]);

  const contextValue = useMemo<NavigationProgressContextValue>(() => ({
    isNavigating,
    startNavigation,
  }), [isNavigating, startNavigation]);

  return (
    <NavigationProgressContext.Provider value={contextValue}>
      {children}
    </NavigationProgressContext.Provider>
  );
}

export function NavigationProgressProvider({ children }: NavigationProgressProviderProps) {
  return (
    <Suspense fallback={(
      <NavigationProgressContext.Provider value={defaultContextValue}>
        {children}
      </NavigationProgressContext.Provider>
    )}
    >
      <NavigationProgressProviderInner>
        {children}
      </NavigationProgressProviderInner>
    </Suspense>
  );
}

export function useNavigationProgress() {
  const context = useContext(NavigationProgressContext);

  if (!context) {
    throw new Error('useNavigationProgress must be used within NavigationProgressProvider');
  }

  return context;
}
