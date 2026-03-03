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

export function canCreateOrder(scopes: readonly string[] | null | undefined): boolean {
  return hasAnyScope(scopes, ['orders.write', 'orders.manage', 'admin.full']);
}

export function canManageUsers(scopes: readonly string[] | null | undefined): boolean {
  return hasAnyScope(scopes, ['admin.users', 'admin.full']);
}

export function canManageRoles(scopes: readonly string[] | null | undefined): boolean {
  return hasAnyScope(scopes, ['admin.roles', 'admin.full']);
}
