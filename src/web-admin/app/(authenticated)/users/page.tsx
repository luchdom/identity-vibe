'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import { AuthGuard } from '@/components/auth/AuthGuard';
import { CreateUserDialog } from '@/components/admin/users/create-user-dialog';
import { EditUserDialog } from '@/components/admin/users/edit-user-dialog';
import { ResetPasswordDialog } from '@/components/admin/users/reset-password-dialog';
import { UserRolesDialog } from '@/components/admin/users/user-roles-dialog';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useAuth } from '@/contexts/auth-context';
import { adminUsersApi, type AdminRoleOption, type AdminUserDetail, type AdminUserListResponse } from '@/lib/admin-users-api';
import { toast } from 'sonner';

const PAGE_SIZE = 20;

function getErrorMessage(error: unknown, fallback: string): string {
  if (typeof error === 'object' && error !== null) {
    const maybeAxios = error as { response?: { data?: { detail?: string } }; message?: string };
    return maybeAxios.response?.data?.detail || maybeAxios.message || fallback;
  }

  return fallback;
}

export default function UsersPage() {
  const { user } = useAuth();
  const currentUserId = useMemo(() => {
    const candidate = user as { id?: string; sub?: string } | null;
    return candidate?.id ?? candidate?.sub ?? null;
  }, [user]);

  const [usersResponse, setUsersResponse] = useState<AdminUserListResponse | null>(null);
  const [availableRoles, setAvailableRoles] = useState<AdminRoleOption[]>([]);
  const [isLoadingUsers, setIsLoadingUsers] = useState(true);
  const [isLoadingRoles, setIsLoadingRoles] = useState(true);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'inactive'>('all');
  const [roleFilter, setRoleFilter] = useState('');
  const [pageNumber, setPageNumber] = useState(1);

  const [selectedUser, setSelectedUser] = useState<AdminUserDetail | null>(null);
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
  const [isRolesDialogOpen, setIsRolesDialogOpen] = useState(false);
  const [isResetPasswordDialogOpen, setIsResetPasswordDialogOpen] = useState(false);
  const [isLoadingSelectedUser, setIsLoadingSelectedUser] = useState(false);

  const loadRoles = useCallback(async () => {
    setIsLoadingRoles(true);
    try {
      const roles = await adminUsersApi.getAvailableRoles();
      setAvailableRoles(roles);
    } catch (error: unknown) {
      toast.error(getErrorMessage(error, 'Failed to load available roles'));
    } finally {
      setIsLoadingRoles(false);
    }
  }, []);

  const loadUsers = useCallback(async () => {
    setIsLoadingUsers(true);
    try {
      const response = await adminUsersApi.listUsers({
        search: search || undefined,
        isActive: statusFilter === 'all' ? undefined : statusFilter === 'active',
        role: roleFilter || undefined,
        pageNumber,
        pageSize: PAGE_SIZE,
      });
      setUsersResponse(response);
    } catch (error: unknown) {
      toast.error(getErrorMessage(error, 'Failed to load users'));
    } finally {
      setIsLoadingUsers(false);
    }
  }, [pageNumber, roleFilter, search, statusFilter]);

  const loadSelectedUser = useCallback(async (userId: string) => {
    setIsLoadingSelectedUser(true);
    try {
      const detail = await adminUsersApi.getUser(userId);
      setSelectedUser(detail);
      return detail;
    } catch (error: unknown) {
      toast.error(getErrorMessage(error, 'Failed to load user details'));
      return null;
    } finally {
      setIsLoadingSelectedUser(false);
    }
  }, []);

  useEffect(() => {
    loadRoles();
  }, [loadRoles]);

  useEffect(() => {
    loadUsers();
  }, [loadUsers]);

  const handleApplyFilters = () => {
    setPageNumber(1);
    setSearch(searchInput.trim());
  };

  const handleClearFilters = () => {
    setSearchInput('');
    setSearch('');
    setStatusFilter('all');
    setRoleFilter('');
    setPageNumber(1);
  };

  const handleCreateUser = async (payload: {
    email: string;
    password: string;
    phoneNumber?: string;
    isActive?: boolean;
    roleNames: string[];
  }) => {
    try {
      await adminUsersApi.createUser(payload);
      toast.success('User created');
      await loadUsers();
    } catch (error: unknown) {
      toast.error(getErrorMessage(error, 'Failed to create user'));
      throw error;
    }
  };

  const openEditDialog = async (userId: string) => {
    const detail = await loadSelectedUser(userId);
    if (!detail) {
      return;
    }
    setIsEditDialogOpen(true);
  };

  const openRolesDialog = async (userId: string) => {
    const detail = await loadSelectedUser(userId);
    if (!detail) {
      return;
    }
    setIsRolesDialogOpen(true);
  };

  const openResetPasswordDialog = async (userId: string) => {
    const detail = await loadSelectedUser(userId);
    if (!detail) {
      return;
    }
    setIsResetPasswordDialogOpen(true);
  };

  const handleSaveUser = async (payload: { email?: string; phoneNumber?: string }) => {
    if (!selectedUser) {
      return;
    }

    try {
      const updated = await adminUsersApi.updateUser(selectedUser.id, payload);
      setSelectedUser(updated);
      toast.success('User updated');
      await loadUsers();
    } catch (error: unknown) {
      toast.error(getErrorMessage(error, 'Failed to update user'));
      throw error;
    }
  };

  const handleSaveRoles = async (roleNames: string[]) => {
    if (!selectedUser) {
      return;
    }

    try {
      const updated = await adminUsersApi.updateUserRoles(selectedUser.id, { roleNames });
      setSelectedUser(updated);
      toast.success('User roles updated');
      await loadUsers();
    } catch (error: unknown) {
      toast.error(getErrorMessage(error, 'Failed to update user roles'));
      throw error;
    }
  };

  const handleResetPassword = async (newPassword: string) => {
    if (!selectedUser) {
      return;
    }

    try {
      const updated = await adminUsersApi.resetUserPassword(selectedUser.id, { newPassword });
      setSelectedUser(updated);
      toast.success('Password reset completed');
      await loadUsers();
    } catch (error: unknown) {
      toast.error(getErrorMessage(error, 'Failed to reset password'));
      throw error;
    }
  };

  const handleToggleStatus = async (userId: string, isActive: boolean) => {
    const nextActive = !isActive;

    if (!nextActive && currentUserId && userId === currentUserId) {
      toast.error('You cannot disable your own account.');
      return;
    }

    try {
      await adminUsersApi.updateUserStatus(userId, { isActive: nextActive });
      toast.success(nextActive ? 'User enabled' : 'User disabled');
      await loadUsers();
      if (selectedUser?.id === userId) {
        setSelectedUser((current) => (current ? { ...current, isActive: nextActive } : current));
      }
    } catch (error: unknown) {
      toast.error(getErrorMessage(error, 'Failed to update user status'));
    }
  };

  const totalUsers = usersResponse?.totalCount ?? 0;
  const activeUsersOnPage = usersResponse?.users.filter((entry) => entry.isActive).length ?? 0;
  const inactiveUsersOnPage = usersResponse?.users.filter((entry) => !entry.isActive).length ?? 0;

  return (
    <AuthGuard requireAuth={true} requiredScopes={['admin.users', 'admin.full']}>
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0" data-testid="users-management-page">
        <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Users</h1>
            <p className="text-muted-foreground">
              Manage accounts, roles, status, and password resets.
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <Badge variant="outline">{totalUsers} total</Badge>
            <Badge variant="secondary">{activeUsersOnPage} active on page</Badge>
            <Badge variant="secondary">{inactiveUsersOnPage} disabled on page</Badge>
            <CreateUserDialog availableRoles={availableRoles} onCreate={handleCreateUser} />
          </div>
        </div>

        <Card>
          <CardHeader>
            <CardTitle>User Accounts</CardTitle>
            <CardDescription>Search and manage users with admin scope enforcement.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid gap-3 md:grid-cols-[1fr_auto_auto_auto_auto]">
              <input
                data-testid="users-search-input"
                className="h-9 rounded-md border px-3 text-sm"
                placeholder="Search by email, username, phone..."
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
              />
              <select
                data-testid="users-status-filter"
                className="h-9 rounded-md border px-3 text-sm"
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value as 'all' | 'active' | 'inactive')}
              >
                <option value="all">All statuses</option>
                <option value="active">Active</option>
                <option value="inactive">Disabled</option>
              </select>
              <select
                data-testid="users-role-filter"
                className="h-9 rounded-md border px-3 text-sm"
                value={roleFilter}
                onChange={(e) => setRoleFilter(e.target.value)}
                disabled={isLoadingRoles}
              >
                <option value="">All roles</option>
                {availableRoles.map((role) => (
                  <option key={role.id} value={role.name}>{role.name}</option>
                ))}
              </select>
              <Button type="button" variant="outline" onClick={handleClearFilters}>
                Clear
              </Button>
              <Button type="button" onClick={handleApplyFilters}>
                Apply
              </Button>
            </div>

            {isLoadingUsers ? (
              <p className="text-sm text-muted-foreground">Loading users...</p>
            ) : usersResponse && usersResponse.users.length > 0 ? (
              <div className="overflow-x-auto rounded-md border">
                <table className="w-full min-w-[860px] text-sm" data-testid="users-table">
                  <thead className="bg-muted/50 text-left">
                    <tr>
                      <th className="px-3 py-2">Email</th>
                      <th className="px-3 py-2">Phone</th>
                      <th className="px-3 py-2">Roles</th>
                      <th className="px-3 py-2">Status</th>
                      <th className="px-3 py-2">Created</th>
                      <th className="px-3 py-2 text-right">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {usersResponse.users.map((entry) => {
                      const isSelf = currentUserId === entry.id;
                      const isDisableAction = entry.isActive;
                      return (
                        <tr key={entry.id} className="border-t">
                          <td className="px-3 py-2">
                            <p className="font-medium">{entry.email}</p>
                            <p className="text-xs text-muted-foreground">{entry.userName}</p>
                          </td>
                          <td className="px-3 py-2">{entry.phoneNumber || '-'}</td>
                          <td className="px-3 py-2">
                            <div className="flex flex-wrap gap-1">
                              {entry.roles.length > 0 ? entry.roles.map((roleName) => (
                                <Badge key={`${entry.id}-${roleName}`} variant="outline">{roleName}</Badge>
                              )) : <span className="text-muted-foreground">No roles</span>}
                            </div>
                          </td>
                          <td className="px-3 py-2">
                            <Badge variant={entry.isActive ? 'secondary' : 'destructive'}>
                              {entry.isActive ? 'Active' : 'Disabled'}
                            </Badge>
                          </td>
                          <td className="px-3 py-2">
                            {new Date(entry.createdAt).toLocaleString()}
                          </td>
                          <td className="px-3 py-2">
                            <div className="flex justify-end gap-2">
                              <Button type="button" size="sm" variant="outline" onClick={() => openEditDialog(entry.id)}>
                                Edit
                              </Button>
                              <Button type="button" size="sm" variant="outline" onClick={() => openRolesDialog(entry.id)}>
                                Roles
                              </Button>
                              <Button type="button" size="sm" variant="outline" onClick={() => openResetPasswordDialog(entry.id)}>
                                Reset Password
                              </Button>
                              <Button
                                type="button"
                                size="sm"
                                variant={entry.isActive ? 'destructive' : 'default'}
                                disabled={isSelf && isDisableAction}
                                onClick={() => handleToggleStatus(entry.id, entry.isActive)}
                                data-testid={`toggle-user-status-${entry.id}`}
                              >
                                {entry.isActive ? 'Disable' : 'Enable'}
                              </Button>
                            </div>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">No users found for the selected filters.</p>
            )}

            {usersResponse && usersResponse.totalPages > 1 ? (
              <div className="flex items-center justify-between text-sm">
                <p className="text-muted-foreground">
                  Page {usersResponse.pageNumber} of {usersResponse.totalPages} ({usersResponse.totalCount} users)
                </p>
                <div className="flex gap-2">
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    disabled={!usersResponse.hasPreviousPage}
                    onClick={() => setPageNumber((current) => Math.max(current - 1, 1))}
                  >
                    Previous
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    disabled={!usersResponse.hasNextPage}
                    onClick={() => setPageNumber((current) => current + 1)}
                  >
                    Next
                  </Button>
                </div>
              </div>
            ) : null}
          </CardContent>
        </Card>

        <EditUserDialog
          user={selectedUser}
          open={isEditDialogOpen}
          onOpenChange={setIsEditDialogOpen}
          onSave={handleSaveUser}
        />

        <UserRolesDialog
          user={selectedUser}
          availableRoles={availableRoles}
          currentUserId={currentUserId ?? undefined}
          open={isRolesDialogOpen}
          onOpenChange={setIsRolesDialogOpen}
          onSave={handleSaveRoles}
        />

        <ResetPasswordDialog
          open={isResetPasswordDialogOpen}
          onOpenChange={setIsResetPasswordDialogOpen}
          onResetPassword={handleResetPassword}
        />

        {isLoadingSelectedUser ? (
          <p className="text-xs text-muted-foreground">Loading selected user details...</p>
        ) : null}
      </div>
    </AuthGuard>
  );
}
