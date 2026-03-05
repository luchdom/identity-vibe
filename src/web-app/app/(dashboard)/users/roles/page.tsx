'use client';

import { useCallback, useEffect, useMemo, useRef, useState } from 'react';

import Alert from '@mui/material/Alert';
import Breadcrumbs from '@mui/material/Breadcrumbs';
import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import Grid from '@mui/material/Grid';
import LinearProgress from '@mui/material/LinearProgress';
import Snackbar from '@mui/material/Snackbar';
import Stack from '@mui/material/Stack';
import Switch from '@mui/material/Switch';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';

import { AppShell } from '@/components/app-shell';
import { CreateRoleDialog, RoleScopeEditor, RolesList } from '@/components/admin/roles';
import { AuthGuard } from '@/components/auth';
import { SurfacePanel } from '@/components/common/surface-panel';
import {
  adminRolesApi,
  type AdminRoleDetail,
  type AdminRoleSummary,
  type AvailableScope,
} from '@/lib/admin-roles-api';
import { isRequestAborted } from '@/lib/api';
import {
  ROLE_DETAIL_CACHE_PREFIX,
  ROLES_AVAILABLE_SCOPES_CACHE_KEY,
  ROLES_LIST_CACHE_PREFIX,
  roleDetailCacheKey,
  rolesListCacheKey,
} from '@/lib/cache/query-keys';
import { queryCache } from '@/lib/cache/query-cache';
import { useDelayedFlag } from '@/hooks/use-delayed-flag';

type FeedbackState = {
  open: boolean;
  severity: 'success' | 'error';
  message: string;
};

function getErrorMessage(error: unknown, fallback: string): string {
  if (typeof error === 'object' && error !== null) {
    const maybeAxios = error as { response?: { data?: { detail?: string } }; message?: string };
    return maybeAxios.response?.data?.detail || maybeAxios.message || fallback;
  }

  return fallback;
}

export default function RolesPermissionsPage() {
  const listAbortRef = useRef<AbortController | null>(null);
  const listRequestIdRef = useRef(0);
  const detailAbortRef = useRef<AbortController | null>(null);
  const detailRequestIdRef = useRef(0);
  const [roles, setRoles] = useState<AdminRoleSummary[]>([]);
  const [availableScopes, setAvailableScopes] = useState<AvailableScope[]>([]);
  const [selectedRoleId, setSelectedRoleId] = useState<string | null>(null);
  const [selectedRole, setSelectedRole] = useState<AdminRoleDetail | null>(null);
  const [search, setSearch] = useState('');
  const [isInitialLoading, setIsInitialLoading] = useState(true);
  const [isRefetching, setIsRefetching] = useState(false);
  const [listLoadError, setListLoadError] = useState<string | null>(null);
  const [isRefreshingRole, setIsRefreshingRole] = useState(false);
  const [isSavingMeta, setIsSavingMeta] = useState(false);
  const [isSavingScopes, setIsSavingScopes] = useState(false);
  const [descriptionDraft, setDescriptionDraft] = useState('');
  const [isActiveDraft, setIsActiveDraft] = useState(true);
  const [selectedScopesDraft, setSelectedScopesDraft] = useState<string[]>([]);
  const [feedback, setFeedback] = useState<FeedbackState>({
    open: false,
    severity: 'success',
    message: '',
  });
  const hasInitializedRef = useRef(false);
  const showSlowRolesListHint = useDelayedFlag(isInitialLoading || isRefetching, 1200);
  const showSlowRoleDetailHint = useDelayedFlag(isRefreshingRole, 1200);

  const showFeedback = useCallback((severity: FeedbackState['severity'], message: string) => {
    setFeedback({
      open: true,
      severity,
      message,
    });
  }, []);

  const filteredRoles = useMemo(() => {
    const query = search.trim().toLowerCase();
    if (!query) {
      return roles;
    }

    return roles.filter((role) =>
      role.name.toLowerCase().includes(query) ||
      (role.description || '').toLowerCase().includes(query)
    );
  }, [roles, search]);

  const syncRoleDrafts = useCallback((role: AdminRoleDetail | null) => {
    setSelectedRole(role);
    setDescriptionDraft(role?.description || '');
    setIsActiveDraft(role?.isActive ?? true);
    setSelectedScopesDraft(role?.assignedScopes ?? []);
  }, []);

  const loadRoleDetail = useCallback(async (roleId: string) => {
    const requestId = ++detailRequestIdRef.current;
    detailAbortRef.current?.abort();
    const controller = new AbortController();
    detailAbortRef.current = controller;

    const detailKey = roleDetailCacheKey(roleId);
    const cachedRole = queryCache.get<AdminRoleDetail>(detailKey);
    if (cachedRole) {
      syncRoleDrafts(cachedRole);
    }

    setIsRefreshingRole(true);
    try {
      const role = await adminRolesApi.getRole(roleId, controller.signal);
      if (requestId !== detailRequestIdRef.current) {
        return null;
      }

      syncRoleDrafts(role);
      queryCache.set(detailKey, role);
      return role;
    } catch (error) {
      if (isRequestAborted(error) || requestId !== detailRequestIdRef.current) {
        return null;
      }

      showFeedback('error', getErrorMessage(error, 'Failed to load role details'));
      return null;
    } finally {
      if (requestId === detailRequestIdRef.current) {
        setIsRefreshingRole(false);
      }
    }
  }, [showFeedback, syncRoleDrafts]);

  const loadInitialData = useCallback(async (preferredRoleId?: string) => {
    const requestId = ++listRequestIdRef.current;
    listAbortRef.current?.abort();
    const controller = new AbortController();
    listAbortRef.current = controller;

    const isFirstLoad = !hasInitializedRef.current;

    if (isFirstLoad) {
      setIsInitialLoading(true);
    } else {
      setIsRefetching(true);
    }

    setListLoadError(null);

    const listCacheKey = rolesListCacheKey();
    const cachedRoles = queryCache.get<AdminRoleSummary[]>(listCacheKey);
    const cachedScopes = queryCache.get<AvailableScope[]>(ROLES_AVAILABLE_SCOPES_CACHE_KEY);

    if (cachedRoles) {
      setRoles(cachedRoles);
    }

    if (cachedScopes) {
      setAvailableScopes(cachedScopes);
    }

    try {
      const [rolesResponse, scopesResponse] = await Promise.all([
        adminRolesApi.listRoles(controller.signal),
        adminRolesApi.getAvailableScopes(controller.signal),
      ]);
      if (requestId !== listRequestIdRef.current) {
        return;
      }

      setRoles(rolesResponse);
      setAvailableScopes(scopesResponse);
      queryCache.set(listCacheKey, rolesResponse);
      queryCache.set(ROLES_AVAILABLE_SCOPES_CACHE_KEY, scopesResponse);

      const nextRoleId = preferredRoleId || rolesResponse[0]?.id || null;
      setSelectedRoleId(nextRoleId);

      if (nextRoleId) {
        await loadRoleDetail(nextRoleId);
      } else {
        syncRoleDrafts(null);
      }
    } catch (error) {
      if (isRequestAborted(error) || requestId !== listRequestIdRef.current) {
        return;
      }

      const message = getErrorMessage(error, 'Failed to load roles and scopes');
      setListLoadError(message);
      showFeedback('error', message);
    } finally {
      if (requestId === listRequestIdRef.current) {
        hasInitializedRef.current = true;
        setIsInitialLoading(false);
        setIsRefetching(false);
      }
    }
  }, [loadRoleDetail, showFeedback, syncRoleDrafts]);

  useEffect(() => {
    void loadInitialData();
  }, [loadInitialData]);

  useEffect(() => () => {
    listAbortRef.current?.abort();
    detailAbortRef.current?.abort();
  }, []);

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
    if (!selectedRoleId) {
      return;
    }

    setIsSavingMeta(true);
    try {
      const updated = await adminRolesApi.updateRole(selectedRoleId, {
        description: descriptionDraft.trim() || undefined,
        isActive: isActiveDraft,
      });

      queryCache.invalidatePrefix(ROLES_LIST_CACHE_PREFIX);
      queryCache.invalidatePrefix(ROLE_DETAIL_CACHE_PREFIX);
      queryCache.set(roleDetailCacheKey(updated.id), updated);

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

      showFeedback('success', 'Role metadata saved');
    } catch (error) {
      showFeedback('error', getErrorMessage(error, 'Failed to save role metadata'));
    } finally {
      setIsSavingMeta(false);
    }
  };

  const handleSaveScopes = async () => {
    if (!selectedRoleId) {
      return;
    }

    setIsSavingScopes(true);
    try {
      const updated = await adminRolesApi.updateRoleScopes(selectedRoleId, {
        scopeNames: selectedScopesDraft,
      });

      queryCache.invalidatePrefix(ROLES_LIST_CACHE_PREFIX);
      queryCache.invalidatePrefix(ROLE_DETAIL_CACHE_PREFIX);
      queryCache.set(roleDetailCacheKey(updated.id), updated);

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

      showFeedback('success', 'Role permissions saved');
    } catch (error) {
      showFeedback('error', getErrorMessage(error, 'Failed to save role permissions'));
    } finally {
      setIsSavingScopes(false);
    }
  };

  const hasMetadataChanges = selectedRole
    ? descriptionDraft.trim() !== (selectedRole.description || '') || isActiveDraft !== selectedRole.isActive
    : false;

  const selectedScopesNormalized = useMemo(
    () => [...selectedScopesDraft].sort().join('|'),
    [selectedScopesDraft]
  );
  const persistedScopesNormalized = useMemo(
    () => [...(selectedRole?.assignedScopes || [])].sort().join('|'),
    [selectedRole]
  );
  const hasScopeChanges = selectedRole
    ? selectedScopesNormalized !== persistedScopesNormalized
    : false;

  return (
    <AuthGuard requiredScopes={['admin.roles', 'admin.full']}>
      <AppShell
        title="Roles & Permissions"
        subtitle="Role catalog and grouped permission scope management."
      >
        <Stack spacing={3} data-testid="roles-permissions-page">
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={2}
            justifyContent="space-between"
            alignItems={{ xs: 'flex-start', sm: 'center' }}
          >
            <Stack spacing={0.75}>
              <Typography variant="h4">List</Typography>
              <Breadcrumbs separator="•" aria-label="breadcrumb">
                <Typography variant="body2" color="text.secondary">
                  Dashboard
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  User
                </Typography>
                <Typography variant="body2" color="text.primary">
                  Roles
                </Typography>
              </Breadcrumbs>
            </Stack>

            <Stack direction="row" spacing={1.25}>
              <Button
                size="small"
                variant="outlined"
                onClick={() => void loadInitialData(selectedRoleId ?? undefined)}
                disabled={isInitialLoading || isRefetching}
                sx={{ height: 36, borderRadius: 1, px: 1.5 }}
              >
                {isRefetching ? 'Refreshing...' : 'Refresh'}
              </Button>
              <CreateRoleDialog
                onCreated={async (roleId) => {
                  queryCache.invalidatePrefix(ROLES_LIST_CACHE_PREFIX);
                  queryCache.invalidatePrefix(ROLE_DETAIL_CACHE_PREFIX);
                  await loadInitialData(roleId);
                  showFeedback('success', 'Role created');
                }}
              />
            </Stack>
          </Stack>

          {listLoadError ? (
            <Alert
              severity="error"
              variant="outlined"
              action={(
                <Button size="small" color="inherit" onClick={() => void loadInitialData(selectedRoleId ?? undefined)}>
                  Retry
                </Button>
              )}
            >
              {listLoadError}
            </Alert>
          ) : null}

          {isRefetching ? (
            <Stack spacing={0.75}>
              <LinearProgress />
              {showSlowRolesListHint ? (
                <Typography variant="caption" color="text.secondary">
                  Refresh is taking longer than expected.
                </Typography>
              ) : null}
            </Stack>
          ) : null}

          <Grid container spacing={2.5}>
            <Grid size={{ xs: 12, lg: 4 }}>
              <RolesList
                roles={filteredRoles}
                selectedRoleId={selectedRoleId}
                isLoading={isInitialLoading}
                onSelectRole={(roleId) => {
                  void handleSelectRole(roleId);
                }}
                search={search}
                onSearchChange={setSearch}
              />
            </Grid>

            <Grid size={{ xs: 12, lg: 8 }}>
              <Stack spacing={2.5}>
                <SurfacePanel>
                  <Stack spacing={2} sx={{ p: 2.5 }} data-testid="role-details-panel">
                    <Stack
                      direction={{ xs: 'column', sm: 'row' }}
                      spacing={1.25}
                      alignItems={{ xs: 'flex-start', sm: 'center' }}
                      justifyContent="space-between"
                    >
                      <Typography variant="subtitle1" fontWeight={700}>
                        Role details
                      </Typography>

                      {isRefreshingRole ? (
                        <Stack direction="row" spacing={1} alignItems="center">
                          <CircularProgress size={16} />
                          <Typography variant="caption" color="text.secondary">
                            {showSlowRoleDetailHint ? 'Still loading role details...' : 'Refreshing role...'}
                          </Typography>
                        </Stack>
                      ) : null}
                    </Stack>

                    {!selectedRole ? (
                      <Typography variant="body2" color="text.secondary">
                        {isInitialLoading ? 'Loading role data...' : 'Select a role to edit.'}
                      </Typography>
                    ) : (
                      <Stack spacing={2}>
                        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                          <TextField
                            size="small"
                            label="Role name"
                            value={selectedRole.name}
                            fullWidth
                            disabled
                          />

                          <Stack
                            direction="row"
                            spacing={1}
                            alignItems="center"
                            data-testid="role-active-toggle"
                            sx={{
                              border: '1px solid',
                              borderColor: 'divider',
                              borderRadius: 1,
                              height: 36,
                              px: 1.25,
                              minWidth: 170,
                            }}
                          >
                            <Switch
                              size="small"
                              checked={isActiveDraft}
                              onChange={(_, checked) => setIsActiveDraft(checked)}
                              disabled={isRefreshingRole || isSavingMeta}
                            />
                            <Typography variant="body2">
                              {isActiveDraft ? 'Active' : 'Disabled'}
                            </Typography>
                          </Stack>
                        </Stack>

                        <TextField
                          size="small"
                          label="Description"
                          multiline
                          minRows={3}
                          value={descriptionDraft}
                          onChange={(event) => setDescriptionDraft(event.target.value)}
                          disabled={isRefreshingRole || isSavingMeta}
                          inputProps={{ 'data-testid': 'role-description-input' }}
                        />

                        <Stack
                          direction={{ xs: 'column', sm: 'row' }}
                          spacing={1}
                          alignItems={{ xs: 'flex-start', sm: 'center' }}
                          justifyContent="space-between"
                        >
                          <Typography variant="caption" color="text.secondary">
                            Created {new Date(selectedRole.createdAt).toLocaleString()}
                            {selectedRole.updatedAt
                              ? ` • Updated ${new Date(selectedRole.updatedAt).toLocaleString()}`
                              : ''}
                          </Typography>
                          <Button
                            size="small"
                            variant="contained"
                            color="inherit"
                            onClick={() => void handleSaveMetadata()}
                            disabled={!hasMetadataChanges || isSavingMeta || isRefreshingRole}
                          >
                            {isSavingMeta ? 'Saving...' : 'Save metadata'}
                          </Button>
                        </Stack>
                      </Stack>
                    )}
                  </Stack>
                </SurfacePanel>

                <RoleScopeEditor
                  availableScopes={availableScopes}
                  selectedScopes={selectedScopesDraft}
                  onToggleScope={handleToggleScope}
                  onReplaceGroup={handleReplaceGroup}
                  disabled={!selectedRole || isRefreshingRole || isSavingScopes}
                />

                <Stack direction="row" spacing={1.25} justifyContent="flex-end">
                  <Button
                    size="small"
                    variant="outlined"
                    disabled={!selectedRole || isRefreshingRole}
                    onClick={() => selectedRole && syncRoleDrafts(selectedRole)}
                    sx={{ height: 36, borderRadius: 1, px: 1.5 }}
                  >
                    Reset
                  </Button>
                  <Button
                    size="small"
                    variant="contained"
                    color="inherit"
                    disabled={!selectedRole || !hasScopeChanges || isSavingScopes || isRefreshingRole}
                    onClick={() => void handleSaveScopes()}
                    data-testid="save-role-scopes-button"
                    sx={{ height: 36, borderRadius: 1, px: 1.5 }}
                  >
                    {isSavingScopes ? 'Saving...' : 'Save permissions'}
                  </Button>
                </Stack>
              </Stack>
            </Grid>
          </Grid>
        </Stack>

        <Snackbar
          open={feedback.open}
          autoHideDuration={4000}
          onClose={(_, reason) => {
            if (reason === 'clickaway') {
              return;
            }
            setFeedback((current) => ({ ...current, open: false }));
          }}
          anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        >
          <Alert
            severity={feedback.severity}
            variant="filled"
            onClose={() => setFeedback((current) => ({ ...current, open: false }))}
            sx={{ width: '100%' }}
          >
            {feedback.message}
          </Alert>
        </Snackbar>
      </AppShell>
    </AuthGuard>
  );
}
