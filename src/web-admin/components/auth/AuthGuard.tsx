'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/contexts/auth-context';
import { LoadingSpinner } from '@/components/ui/loading-spinner';
import { hasAllRoles, hasAnyRole, hasAnyScope, isAdminByScope } from '@/lib/authz';

interface AuthGuardProps {
  children: React.ReactNode;
  requireAuth?: boolean;
  redirectTo?: string;
  requiredRoles?: string[];
  requiredScopes?: string[];
  fallbackComponent?: React.ReactNode;
}

export function AuthGuard({
  children,
  requireAuth = true,
  redirectTo = '/login',
  requiredRoles = [],
  requiredScopes = [],
  fallbackComponent = null
}: AuthGuardProps) {
  const { isAuthenticated, isLoading, user, scopes } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (isLoading) return;

    if (requireAuth && !isAuthenticated) {
      router.push(redirectTo);
      return;
    }

    if (!requireAuth && isAuthenticated) {
      router.push('/dashboard');
      return;
    }

    if (requireAuth && isAuthenticated && requiredRoles.length > 0 && !hasAnyRole(user, requiredRoles)) {
      router.push('/unauthorized');
      return;
    }

    if (requireAuth && isAuthenticated && requiredScopes.length > 0 && !hasAnyScope(scopes, requiredScopes)) {
      router.push('/unauthorized');
    }
  }, [isAuthenticated, isLoading, user, scopes, requireAuth, router, redirectTo, requiredRoles, requiredScopes]);

  if (isLoading) {
    return <LoadingSpinner message="Checking authentication..." />;
  }

  if (requireAuth && !isAuthenticated) {
    return fallbackComponent || <LoadingSpinner message="Redirecting to login..." />;
  }

  if (!requireAuth && isAuthenticated) {
    return fallbackComponent || <LoadingSpinner message="Redirecting..." />;
  }

  if (requireAuth && isAuthenticated && requiredRoles.length > 0 && !hasAnyRole(user, requiredRoles)) {
    return fallbackComponent || (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-center">
          <h2 className="text-2xl font-bold text-gray-900 mb-4">Access Denied</h2>
          <p className="text-gray-600 mb-4">You do not have permission to access this page.</p>
          <p className="text-sm text-gray-500">Required roles: {requiredRoles.join(', ')}</p>
        </div>
      </div>
    );
  }

  if (requireAuth && isAuthenticated && requiredScopes.length > 0 && !hasAnyScope(scopes, requiredScopes)) {
    return fallbackComponent || (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-center">
          <h2 className="text-2xl font-bold text-gray-900 mb-4">Access Denied</h2>
          <p className="text-gray-600 mb-4">You do not have permission to access this page.</p>
          <p className="text-sm text-gray-500">Required scopes: {requiredScopes.join(', ')}</p>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}

export { hasAllRoles, hasAnyScope as hasAnyScopes };

export function isAdmin(_user: { roles?: string[] } | null | undefined, scopes?: string[]): boolean {
  return isAdminByScope(scopes);
}

export function withAuthGuard<P extends object>(
  Component: React.ComponentType<P>,
  options: Omit<AuthGuardProps, 'children'>
) {
  return function ProtectedComponent(props: P) {
    return (
      <AuthGuard {...options}>
        <Component {...props} />
      </AuthGuard>
    );
  };
}
