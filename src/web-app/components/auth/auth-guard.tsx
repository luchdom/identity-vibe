'use client';

import { useEffect, type ReactNode } from 'react';
import { usePathname, useRouter } from 'next/navigation';

import Box from '@mui/material/Box';
import CircularProgress from '@mui/material/CircularProgress';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import { useAuth } from '@/contexts/auth-context';
import { hasAnyScope } from '@/lib/permissions/authz';

type AuthGuardProps = {
  children: ReactNode;
  requireAuth?: boolean;
  requiredScopes?: string[];
};

function AuthStatusScreen({ message }: { message: string }) {
  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'grid',
        placeItems: 'center',
        px: 3,
      }}
    >
      <Stack spacing={2} alignItems="center">
        <CircularProgress size={28} />
        <Typography color="text.secondary">{message}</Typography>
      </Stack>
    </Box>
  );
}

export function AuthGuard({
  children,
  requireAuth = true,
  requiredScopes = [],
}: AuthGuardProps) {
  const { isAuthenticated, isLoading, scopes } = useAuth();
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    if (isLoading) {
      return;
    }

    if (!requireAuth && isAuthenticated) {
      router.replace('/dashboard');
      return;
    }

    if (requireAuth && !isAuthenticated) {
      const query = typeof window === 'undefined'
        ? ''
        : new URLSearchParams(window.location.search).toString();
      const returnUrl = query ? `${pathname}?${query}` : pathname;

      if (pathname !== '/login' && pathname !== '/callback') {
        sessionStorage.setItem('returnUrl', returnUrl);
      }

      router.replace('/login');
      return;
    }

    if (requireAuth && requiredScopes.length > 0 && !hasAnyScope(scopes, requiredScopes)) {
      router.replace('/unauthorized');
    }
  }, [isAuthenticated, isLoading, pathname, requireAuth, requiredScopes, router, scopes]);

  if (isLoading) {
    return <AuthStatusScreen message="Checking authentication..." />;
  }

  if (!requireAuth && isAuthenticated) {
    return <AuthStatusScreen message="Redirecting to dashboard..." />;
  }

  if (requireAuth && !isAuthenticated) {
    return <AuthStatusScreen message="Redirecting to login..." />;
  }

  if (requireAuth && requiredScopes.length > 0 && !hasAnyScope(scopes, requiredScopes)) {
    return <AuthStatusScreen message="Checking access..." />;
  }

  return <>{children}</>;
}
