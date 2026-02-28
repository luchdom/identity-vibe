'use client';

type RolePrincipal = {
  roles?: string[] | null;
} | null | undefined;

export function hasScope(
  scopes: readonly string[] | null | undefined,
  requiredScope: string
): boolean {
  if (!Array.isArray(scopes) || !requiredScope) {
    return false;
  }

  return scopes.includes(requiredScope);
}

export function hasAnyScope(
  scopes: readonly string[] | null | undefined,
  requiredScopes: readonly string[]
): boolean {
  if (!Array.isArray(scopes) || !Array.isArray(requiredScopes) || requiredScopes.length === 0) {
    return false;
  }

  return requiredScopes.some((scope) => scopes.includes(scope));
}

export function hasAnyRole(
  user: RolePrincipal,
  requiredRoles: readonly string[]
): boolean {
  const roles = user?.roles;
  if (!Array.isArray(roles) || !Array.isArray(requiredRoles) || requiredRoles.length === 0) {
    return false;
  }

  return requiredRoles.some((role) => roles.includes(role));
}

export function hasAllRoles(
  user: RolePrincipal,
  requiredRoles: readonly string[]
): boolean {
  const roles = user?.roles;
  if (!Array.isArray(roles) || !Array.isArray(requiredRoles) || requiredRoles.length === 0) {
    return false;
  }

  return requiredRoles.every((role) => roles.includes(role));
}

export function canCreateOrder(scopes: readonly string[] | null | undefined): boolean {
  return hasAnyScope(scopes, ['orders.write', 'orders.manage', 'admin.full']);
}

export function canViewAllOrders(scopes: readonly string[] | null | undefined): boolean {
  return hasAnyScope(scopes, ['orders.manage', 'admin.full']);
}

export function canManageRoles(scopes: readonly string[] | null | undefined): boolean {
  return hasAnyScope(scopes, ['admin.roles', 'admin.full']);
}

export function canAccessAdminPanel(scopes: readonly string[] | null | undefined): boolean {
  return hasScope(scopes, 'admin.full');
}

export function isAdminByScope(scopes: readonly string[] | null | undefined): boolean {
  return hasAnyScope(scopes, ['admin.full', 'orders.manage']);
}

export function getStoredGrantedScopes(): string[] {
  if (typeof window === 'undefined') {
    return [];
  }

  const scopeValue = localStorage.getItem('grantedScopes');
  if (!scopeValue) {
    return [];
  }

  return scopeValue
    .split(' ')
    .map((scope) => scope.trim())
    .filter(Boolean);
}

export function getStoredUser(): Record<string, unknown> | null {
  if (typeof window === 'undefined') {
    return null;
  }

  const userValue = localStorage.getItem('user');
  if (!userValue) {
    return null;
  }

  try {
    return JSON.parse(userValue) as Record<string, unknown>;
  } catch {
    return null;
  }
}
