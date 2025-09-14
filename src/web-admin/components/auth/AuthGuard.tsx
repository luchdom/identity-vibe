'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/contexts/auth-context';
import { LoadingSpinner } from '@/components/ui/loading-spinner';

interface AuthGuardProps {
  children: React.ReactNode;
  requireAuth?: boolean;
  redirectTo?: string;
  requiredRoles?: string[];
  fallbackComponent?: React.ReactNode;
}

export function AuthGuard({
  children,
  requireAuth = true,
  redirectTo = '/login',
  requiredRoles = [],
  fallbackComponent = null
}: AuthGuardProps) {
  const { isAuthenticated, isLoading, user } = useAuth();
  const router = useRouter();

  useEffect(() => {
    // Wait for auth state to load
    if (isLoading) return;

    // Handle unauthenticated access to protected routes
    if (requireAuth && !isAuthenticated) {
      console.log('AuthGuard: Redirecting to login - not authenticated');
      router.push(redirectTo);
      return;
    }

    // Handle authenticated access to public-only routes (like login)
    if (!requireAuth && isAuthenticated) {
      console.log('AuthGuard: Redirecting to dashboard - already authenticated');
      router.push('/dashboard');
      return;
    }

    // Handle role-based access control
    if (requireAuth && isAuthenticated && requiredRoles.length > 0) {
      const hasRequiredRole = hasAnyRole(user, requiredRoles);
      if (!hasRequiredRole) {
        console.log('AuthGuard: Redirecting to unauthorized - insufficient permissions');
        router.push('/unauthorized');
        return;
      }
    }
  }, [isAuthenticated, isLoading, user, requireAuth, router, redirectTo, requiredRoles]);

  // Show loading spinner while auth state is loading
  if (isLoading) {
    return <LoadingSpinner message="Checking authentication..." />;
  }

  // Block rendering if requirements not met
  if (requireAuth && !isAuthenticated) {
    return fallbackComponent || <LoadingSpinner message="Redirecting to login..." />;
  }

  if (!requireAuth && isAuthenticated) {
    return fallbackComponent || <LoadingSpinner message="Redirecting..." />;
  }

  // Check role permissions for authenticated users
  if (requireAuth && isAuthenticated && requiredRoles.length > 0) {
    const hasPermission = hasAnyRole(user, requiredRoles);
    if (!hasPermission) {
      return fallbackComponent || (
        <div className="flex items-center justify-center min-h-screen">
          <div className="text-center">
            <h2 className="text-2xl font-bold text-gray-900 mb-4">Access Denied</h2>
            <p className="text-gray-600 mb-4">
              You don't have permission to access this page.
            </p>
            <p className="text-sm text-gray-500">
              Required roles: {requiredRoles.join(', ')}
            </p>
          </div>
        </div>
      );
    }
  }

  // Render protected content
  return <>{children}</>;
}

/**
 * Check if user has any of the required roles
 */
function hasAnyRole(user: any, requiredRoles: string[]): boolean {
  if (!user?.roles || !Array.isArray(user.roles)) {
    return false;
  }

  return requiredRoles.some(role => user.roles.includes(role));
}

/**
 * Check if user has all of the required roles
 */
export function hasAllRoles(user: any, requiredRoles: string[]): boolean {
  if (!user?.roles || !Array.isArray(user.roles)) {
    return false;
  }

  return requiredRoles.every(role => user.roles.includes(role));
}

/**
 * Check if user is admin (has Admin or ServiceIdentity role)
 */
export function isAdmin(user: any): boolean {
  return hasAnyRole(user, ['Admin', 'ServiceIdentity']);
}

/**
 * Higher-order component for role-based protection
 */
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