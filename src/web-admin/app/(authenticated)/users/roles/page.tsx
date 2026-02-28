'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import { toast } from 'sonner';
import { AuthGuard } from '@/components/auth/AuthGuard';
import { CreateRoleDialog } from '@/components/admin/roles/create-role-dialog';
import { RolesList } from '@/components/admin/roles/roles-list';
import { RoleScopeEditor } from '@/components/admin/roles/scope-editor';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { adminRolesApi, type AdminRoleDetail, type AdminRoleSummary, type AvailableScope } from '@/lib/admin-roles-api';
import { useAuth } from '@/contexts/auth-context';

function getErrorMessage(error: unknown, fallback: string): string {
  if (typeof error === 'object' && error !== null) {
    const maybeAxios = error as { response?: { data?: { detail?: string } }; message?: string };
    return maybeAxios.response?.data?.detail || maybeAxios.message || fallback;
  }

  return fallback;
}

export default function RolesPermissionsPage() {
  const { user, scopes } = useAuth();
  const [roles, setRoles] = useState<AdminRoleSummary[]>([]);
  const [availableScopes, setAvailableScopes] = useState<AvailableScope[]>([]);
  const [selectedRoleId, setSelectedRoleId] = useState<string | null>(null);
  const [selectedRole, setSelectedRole] = useState<AdminRoleDetail | null>(null);
  const [search, setSearch] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshingRole, setIsRefreshingRole] = useState(false);
  const [isSavingMeta, setIsSavingMeta] = useState(false);
  const [isSavingScopes, setIsSavingScopes] = useState(false);

  const [descriptionDraft, setDescriptionDraft] = useState('');
  const [isActiveDraft, setIsActiveDraft] = useState(true);
  const [selectedScopesDraft, setSelectedScopesDraft] = useState<string[]>([]);

  const filteredRoles = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return roles;
    return roles.filter((role) =>
      role.name.toLowerCase().includes(q) || (role.description || '').toLowerCase().includes(q)
    );
  }, [roles, search]);

  const syncRoleDrafts = useCallback((role: AdminRoleDetail | null) => {
    setSelectedRole(role);
    setDescriptionDraft(role?.description || '');
    setIsActiveDraft(role?.isActive ?? true);
    setSelectedScopesDraft(role?.assignedScopes ?? []);
  }, []);

  const loadRoleDetail = useCallback(async (roleId: string) => {
    setIsRefreshingRole(true);
    try {
      const role = await adminRolesApi.getRole(roleId);
      syncRoleDrafts(role);
    } catch (error: unknown) {
      toast.error(getErrorMessage(error, 'Failed to load role'));
    } finally {
      setIsRefreshingRole(false);
    }
  }, [syncRoleDrafts]);

  const loadInitialData = useCallback(async (preferredRoleId?: string) => {
    setIsLoading(true);
    try {
      const [rolesResponse, scopesResponse] = await Promise.all([
        adminRolesApi.listRoles(),
        adminRolesApi.getAvailableScopes(),
      ]);

      setRoles(rolesResponse);
      setAvailableScopes(scopesResponse);

      const nextRoleId = preferredRoleId || rolesResponse[0]?.id || null;

      setSelectedRoleId(nextRoleId);
      if (nextRoleId) {
        await loadRoleDetail(nextRoleId);
      } else {
        syncRoleDrafts(null);
      }
    } catch (error: unknown) {
      toast.error(getErrorMessage(error, 'Failed to load roles and permissions'));
    } finally {
      setIsLoading(false);
    }
  }, [loadRoleDetail, syncRoleDrafts]);

  useEffect(() => {
    loadInitialData();
  }, [loadInitialData]);

  const handleSelectRole = async (roleId: string) => {
    setSelectedRoleId(roleId);
    await loadRoleDetail(roleId);
  };

  const handleToggleScope = (scopeName: string) => {
    setSelectedScopesDraft((current) =>
      current.includes(scopeName)
        ? current.filter((scope) => scope !== scopeName)
        : [...current, scopeName].sort()
    );
  };

  const handleReplaceGroup = (_group: string, nextScopes: string[]) => {
    setSelectedScopesDraft(Array.from(new Set(nextScopes)).sort());
  };

  const handleSaveMetadata = async () => {
    if (!selectedRoleId) return;

    setIsSavingMeta(true);
    try {
      const updated = await adminRolesApi.updateRole(selectedRoleId, {
        description: descriptionDraft.trim() || undefined,
        isActive: isActiveDraft,
      });
      syncRoleDrafts(updated);
      setRoles((current) => current.map((role) =>
        role.id === updated.id
          ? {
              ...role,
              description: updated.description,
              isActive: updated.isActive,
              assignedScopes: updated.assignedScopes,
              assignedScopeCount: updated.assignedScopes.length,
            }
          : role
      ));
      toast.success('Role metadata saved');
    } catch (error: unknown) {
      toast.error(getErrorMessage(error, 'Failed to save role metadata'));
    } finally {
      setIsSavingMeta(false);
    }
  };

  const handleSaveScopes = async () => {
    if (!selectedRoleId) return;

    setIsSavingScopes(true);
    try {
      const updated = await adminRolesApi.updateRoleScopes(selectedRoleId, {
        scopeNames: selectedScopesDraft,
      });
      syncRoleDrafts(updated);
      setRoles((current) => current.map((role) =>
        role.id === updated.id
          ? {
              ...role,
              assignedScopes: updated.assignedScopes,
              assignedScopeCount: updated.assignedScopes.length,
              isActive: updated.isActive,
              description: updated.description,
            }
          : role
      ));
      toast.success('Role permissions saved');
    } catch (error: unknown) {
      toast.error(getErrorMessage(error, 'Failed to save role permissions'));
    } finally {
      setIsSavingScopes(false);
    }
  };

  const hasMetadataChanges = selectedRole
    ? (descriptionDraft.trim() !== (selectedRole.description || '') || isActiveDraft !== selectedRole.isActive)
    : false;

  const selectedScopesNormalized = useMemo(
    () => [...selectedScopesDraft].sort().join('|'),
    [selectedScopesDraft]
  );
  const persistedScopesNormalized = useMemo(
    () => [...(selectedRole?.assignedScopes || [])].sort().join('|'),
    [selectedRole]
  );
  const hasScopeChanges = selectedRole ? selectedScopesNormalized !== persistedScopesNormalized : false;

  return (
    <AuthGuard requireAuth={true} requiredScopes={['admin.roles', 'admin.full']}>
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0" data-testid="roles-permissions-page">
        <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Roles & Permissions</h1>
            <p className="text-muted-foreground">
              Create roles, soft-disable them, and assign permission scopes used by API policies.
            </p>
          </div>
          <div className="flex items-center gap-2">
            <Badge variant="outline">Scope-gated UI</Badge>
            {user?.roles?.length ? <Badge variant="secondary">{user.roles.join(', ')}</Badge> : null}
            <CreateRoleDialog onCreated={(roleId) => loadInitialData(roleId)} />
            <Button type="button" variant="outline" onClick={() => loadInitialData(selectedRoleId ?? undefined)}>
              Refresh
            </Button>
          </div>
        </div>

        <div className="grid gap-4 lg:grid-cols-[360px_minmax(0,1fr)]">
          <RolesList
            roles={filteredRoles}
            selectedRoleId={selectedRoleId}
            isLoading={isLoading}
            onSelectRole={handleSelectRole}
            search={search}
            onSearchChange={setSearch}
          />

          <div className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Role Details</CardTitle>
                <CardDescription>
                  Metadata for the selected role. Role names are immutable in v1.
                </CardDescription>
              </CardHeader>
              <CardContent>
                {!selectedRole ? (
                  <div className="text-sm text-muted-foreground">
                    {isLoading ? 'Loading...' : 'Select a role to edit.'}
                  </div>
                ) : (
                  <div className="space-y-4" data-testid="role-details-panel">
                    <div className="grid gap-4 md:grid-cols-2">
                      <div className="space-y-1">
                        <label className="text-sm font-medium">Role name</label>
                        <input className="h-9 w-full rounded-md border px-3 text-sm bg-muted" value={selectedRole.name} readOnly />
                      </div>
                      <div className="space-y-1">
                        <label className="text-sm font-medium">Status</label>
                        <label className="flex h-9 items-center gap-2 rounded-md border px-3 text-sm">
                          <input
                            data-testid="role-active-toggle"
                            type="checkbox"
                            checked={isActiveDraft}
                            onChange={(e) => setIsActiveDraft(e.target.checked)}
                            disabled={isRefreshingRole || isSavingMeta}
                          />
                          <span>{isActiveDraft ? 'Active' : 'Disabled'}</span>
                        </label>
                      </div>
                    </div>

                    <div className="space-y-1">
                      <label className="text-sm font-medium">Description</label>
                      <textarea
                        data-testid="role-description-input"
                        className="min-h-24 w-full rounded-md border px-3 py-2 text-sm"
                        value={descriptionDraft}
                        onChange={(e) => setDescriptionDraft(e.target.value)}
                        disabled={isRefreshingRole || isSavingMeta}
                      />
                    </div>

                    <div className="flex flex-wrap items-center justify-between gap-2 text-xs text-muted-foreground">
                      <div>
                        Created {new Date(selectedRole.createdAt).toLocaleString()}
                        {selectedRole.updatedAt ? ` • Updated ${new Date(selectedRole.updatedAt).toLocaleString()}` : ''}
                      </div>
                      <Button
                        type="button"
                        size="sm"
                        onClick={handleSaveMetadata}
                        disabled={!hasMetadataChanges || isSavingMeta || isRefreshingRole}
                      >
                        {isSavingMeta ? 'Saving...' : 'Save Metadata'}
                      </Button>
                    </div>
                  </div>
                )}
              </CardContent>
            </Card>

            <RoleScopeEditor
              availableScopes={availableScopes}
              selectedScopes={selectedScopesDraft}
              onToggleScope={handleToggleScope}
              onReplaceGroup={handleReplaceGroup}
              disabled={!selectedRole || isRefreshingRole || isSavingScopes}
            />

            <div className="flex justify-end gap-2">
              <Button
                type="button"
                variant="outline"
                disabled={!selectedRole || isRefreshingRole}
                onClick={() => selectedRole && syncRoleDrafts(selectedRole)}
              >
                Reset Changes
              </Button>
              <Button
                type="button"
                data-testid="save-role-scopes-button"
                disabled={!selectedRole || !hasScopeChanges || isSavingScopes || isRefreshingRole}
                onClick={handleSaveScopes}
              >
                {isSavingScopes ? 'Saving...' : 'Save Permissions'}
              </Button>
            </div>
          </div>
        </div>

        <div className="text-xs text-muted-foreground">
          Current UI scopes: {scopes.join(', ') || 'none'}
        </div>
      </div>
    </AuthGuard>
  );
}
