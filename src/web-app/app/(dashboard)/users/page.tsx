'use client';

import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import Link from 'next/link';

import { Icon } from '@iconify/react';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Breadcrumbs from '@mui/material/Breadcrumbs';
import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import Divider from '@mui/material/Divider';
import FormControlLabel from '@mui/material/FormControlLabel';
import IconButton from '@mui/material/IconButton';
import InputAdornment from '@mui/material/InputAdornment';
import LinearProgress from '@mui/material/LinearProgress';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import OutlinedInput from '@mui/material/OutlinedInput';
import Snackbar from '@mui/material/Snackbar';
import Stack from '@mui/material/Stack';
import Switch from '@mui/material/Switch';
import Tab from '@mui/material/Tab';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TablePagination from '@mui/material/TablePagination';
import TableRow from '@mui/material/TableRow';
import TableSortLabel from '@mui/material/TableSortLabel';
import Tabs from '@mui/material/Tabs';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';

import { AppShell } from '@/components/app-shell';
import {
  CreateUserDialog,
  EditUserDialog,
  ResetPasswordDialog,
  UserRolesDialog,
} from '@/components/admin/users';
import { AuthGuard } from '@/components/auth';
import { AppLabel } from '@/components/common/app-label';
import { EmptyStateCard } from '@/components/common/empty-state-card';
import { SurfacePanel } from '@/components/common/surface-panel';
import { useAuth } from '@/contexts/auth-context';
import {
  adminUsersApi,
  type AdminRoleOption,
  type AdminUserDetail,
  type AdminUserListResponse,
  type AdminUserSummary,
} from '@/lib/admin-users-api';
import { isRequestAborted } from '@/lib/api';
import {
  USERS_AVAILABLE_ROLES_CACHE_KEY,
  USERS_LIST_CACHE_PREFIX,
  usersListCacheKey,
} from '@/lib/cache/query-keys';
import { queryCache } from '@/lib/cache/query-cache';
import { useDelayedFlag } from '@/hooks/use-delayed-flag';

type StatusFilter = 'all' | 'active' | 'inactive';
type SortDirection = 'asc' | 'desc';
type SortKey = 'email' | 'phone' | 'roles' | 'status' | 'createdAt';

type FeedbackState = {
  open: boolean;
  severity: 'success' | 'error';
  message: string;
};

type ActionMenuState = {
  anchorEl: HTMLElement | null;
  userId: string | null;
};

function getErrorMessage(error: unknown, fallback: string): string {
  if (typeof error === 'object' && error !== null) {
    const maybeAxios = error as { response?: { data?: { detail?: string } }; message?: string };
    return maybeAxios.response?.data?.detail || maybeAxios.message || fallback;
  }

  return fallback;
}

function compareUsers(
  left: AdminUserSummary,
  right: AdminUserSummary,
  sortKey: SortKey,
  direction: SortDirection
) {
  const multiplier = direction === 'asc' ? 1 : -1;

  const leftValue = (() => {
    switch (sortKey) {
      case 'email':
        return left.email.toLowerCase();
      case 'phone':
        return (left.phoneNumber || '').toLowerCase();
      case 'roles':
        return left.roles.join('|').toLowerCase();
      case 'status':
        return left.isActive ? 'active' : 'inactive';
      case 'createdAt':
        return new Date(left.createdAt).getTime();
      default:
        return left.email.toLowerCase();
    }
  })();

  const rightValue = (() => {
    switch (sortKey) {
      case 'email':
        return right.email.toLowerCase();
      case 'phone':
        return (right.phoneNumber || '').toLowerCase();
      case 'roles':
        return right.roles.join('|').toLowerCase();
      case 'status':
        return right.isActive ? 'active' : 'inactive';
      case 'createdAt':
        return new Date(right.createdAt).getTime();
      default:
        return right.email.toLowerCase();
    }
  })();

  if (leftValue < rightValue) {
    return -1 * multiplier;
  }

  if (leftValue > rightValue) {
    return 1 * multiplier;
  }

  return 0;
}

export default function UsersPage() {
  const { user } = useAuth();
  const usersAbortRef = useRef<AbortController | null>(null);
  const usersRequestIdRef = useRef(0);
  const rolesAbortRef = useRef<AbortController | null>(null);
  const rolesRequestIdRef = useRef(0);

  const currentUserId = useMemo(() => {
    const candidate = user as { id?: string; sub?: string } | null;
    return candidate?.id ?? candidate?.sub ?? null;
  }, [user]);

  const [usersResponse, setUsersResponse] = useState<AdminUserListResponse | null>(null);
  const [availableRoles, setAvailableRoles] = useState<AdminRoleOption[]>([]);
  const [isLoadingUsers, setIsLoadingUsers] = useState(true);
  const [isLoadingRoles, setIsLoadingRoles] = useState(true);
  const [usersLoadError, setUsersLoadError] = useState<string | null>(null);
  const [rolesLoadError, setRolesLoadError] = useState<string | null>(null);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [roleFilter, setRoleFilter] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const [rowsPerPage, setRowsPerPage] = useState(5);
  const [sortKey, setSortKey] = useState<SortKey>('email');
  const [sortDirection, setSortDirection] = useState<SortDirection>('asc');
  const [dense, setDense] = useState(false);
  const [selectedUser, setSelectedUser] = useState<AdminUserDetail | null>(null);
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
  const [isRolesDialogOpen, setIsRolesDialogOpen] = useState(false);
  const [isResetPasswordDialogOpen, setIsResetPasswordDialogOpen] = useState(false);
  const [isLoadingSelectedUser, setIsLoadingSelectedUser] = useState(false);
  const [feedback, setFeedback] = useState<FeedbackState>({
    open: false,
    severity: 'success',
    message: '',
  });
  const [actionMenu, setActionMenu] = useState<ActionMenuState>({
    anchorEl: null,
    userId: null,
  });

  const showFeedback = useCallback((severity: FeedbackState['severity'], message: string) => {
    setFeedback({
      open: true,
      severity,
      message,
    });
  }, []);

  const loadRoles = useCallback(async () => {
    const requestId = ++rolesRequestIdRef.current;
    rolesAbortRef.current?.abort();
    const controller = new AbortController();
    rolesAbortRef.current = controller;

    setIsLoadingRoles(true);
    setRolesLoadError(null);

    const cachedRoles = queryCache.get<AdminRoleOption[]>(USERS_AVAILABLE_ROLES_CACHE_KEY);
    if (cachedRoles) {
      setAvailableRoles(cachedRoles);
    }

    try {
      const roles = await adminUsersApi.getAvailableRoles(controller.signal);
      if (requestId !== rolesRequestIdRef.current) {
        return;
      }

      setAvailableRoles(roles);
      queryCache.set(USERS_AVAILABLE_ROLES_CACHE_KEY, roles);
    } catch (error) {
      if (isRequestAborted(error) || requestId !== rolesRequestIdRef.current) {
        return;
      }

      const message = getErrorMessage(error, 'Failed to load available roles');
      setRolesLoadError(message);
      showFeedback('error', message);
    } finally {
      if (requestId === rolesRequestIdRef.current) {
        setIsLoadingRoles(false);
      }
    }
  }, [showFeedback]);

  const loadUsers = useCallback(async () => {
    const requestId = ++usersRequestIdRef.current;
    usersAbortRef.current?.abort();
    const controller = new AbortController();
    usersAbortRef.current = controller;

    const listKey = usersListCacheKey({
      search: search || undefined,
      isActive: statusFilter === 'all' ? undefined : statusFilter === 'active',
      role: roleFilter || undefined,
      pageNumber,
      pageSize: rowsPerPage,
    });
    const cachedResponse = queryCache.get<AdminUserListResponse>(listKey);

    if (cachedResponse) {
      setUsersResponse(cachedResponse);
    }

    setIsLoadingUsers(true);
    setUsersLoadError(null);

    try {
      const response = await adminUsersApi.listUsers({
        search: search || undefined,
        isActive: statusFilter === 'all' ? undefined : statusFilter === 'active',
        role: roleFilter || undefined,
        pageNumber,
        pageSize: rowsPerPage,
      }, controller.signal);
      if (requestId !== usersRequestIdRef.current) {
        return;
      }

      setUsersResponse(response);
      queryCache.set(listKey, response);
    } catch (error) {
      if (isRequestAborted(error) || requestId !== usersRequestIdRef.current) {
        return;
      }

      const message = getErrorMessage(error, 'Failed to load users');
      setUsersLoadError(message);
      showFeedback('error', message);
    } finally {
      if (requestId === usersRequestIdRef.current) {
        setIsLoadingUsers(false);
      }
    }
  }, [pageNumber, roleFilter, rowsPerPage, search, showFeedback, statusFilter]);

  const loadSelectedUser = useCallback(async (userId: string) => {
    setIsLoadingSelectedUser(true);

    try {
      const detail = await adminUsersApi.getUser(userId);
      setSelectedUser(detail);
      return detail;
    } catch (error) {
      showFeedback('error', getErrorMessage(error, 'Failed to load user details'));
      return null;
    } finally {
      setIsLoadingSelectedUser(false);
    }
  }, [showFeedback]);

  useEffect(() => {
    void loadRoles();
  }, [loadRoles]);

  useEffect(() => {
    void loadUsers();
  }, [loadUsers]);

  useEffect(() => () => {
    usersAbortRef.current?.abort();
    rolesAbortRef.current?.abort();
  }, []);

  const sortedUsers = useMemo(() => {
    const users = [...(usersResponse?.users ?? [])];
    users.sort((left, right) => compareUsers(left, right, sortKey, sortDirection));
    return users;
  }, [sortDirection, sortKey, usersResponse?.users]);

  const selectedMenuUser = useMemo(
    () => usersResponse?.users.find((entry) => entry.id === actionMenu.userId) ?? null,
    [actionMenu.userId, usersResponse]
  );

  const totalUsers = usersResponse?.totalCount ?? 0;
  const activeUsersOnPage = usersResponse?.users.filter((entry) => entry.isActive).length ?? 0;
  const inactiveUsersOnPage = usersResponse?.users.filter((entry) => !entry.isActive).length ?? 0;
  const isInitialLoadingUsers = isLoadingUsers && !usersResponse;
  const isRefetchingUsers = isLoadingUsers && Boolean(usersResponse);
  const showSlowUsersHint = useDelayedFlag(isLoadingUsers, 1200);

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
      queryCache.invalidatePrefix(USERS_LIST_CACHE_PREFIX);
      showFeedback('success', 'User created');
      await loadUsers();
    } catch (error) {
      showFeedback('error', getErrorMessage(error, 'Failed to create user'));
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
      queryCache.invalidatePrefix(USERS_LIST_CACHE_PREFIX);
      setSelectedUser(updated);
      showFeedback('success', 'User updated');
      await loadUsers();
    } catch (error) {
      showFeedback('error', getErrorMessage(error, 'Failed to update user'));
      throw error;
    }
  };

  const handleSaveRoles = async (roleNames: string[]) => {
    if (!selectedUser) {
      return;
    }

    try {
      const updated = await adminUsersApi.updateUserRoles(selectedUser.id, { roleNames });
      queryCache.invalidatePrefix(USERS_LIST_CACHE_PREFIX);
      setSelectedUser(updated);
      showFeedback('success', 'User roles updated');
      await loadUsers();
    } catch (error) {
      showFeedback('error', getErrorMessage(error, 'Failed to update user roles'));
      throw error;
    }
  };

  const handleResetPassword = async (newPassword: string) => {
    if (!selectedUser) {
      return;
    }

    try {
      const updated = await adminUsersApi.resetUserPassword(selectedUser.id, { newPassword });
      queryCache.invalidatePrefix(USERS_LIST_CACHE_PREFIX);
      setSelectedUser(updated);
      showFeedback('success', 'Password reset completed');
      await loadUsers();
    } catch (error) {
      showFeedback('error', getErrorMessage(error, 'Failed to reset password'));
      throw error;
    }
  };

  const handleToggleStatus = async (userId: string, isActive: boolean) => {
    const nextActive = !isActive;

    if (!nextActive && currentUserId && userId === currentUserId) {
      showFeedback('error', 'You cannot disable your own account.');
      return;
    }

    try {
      await adminUsersApi.updateUserStatus(userId, { isActive: nextActive });
      queryCache.invalidatePrefix(USERS_LIST_CACHE_PREFIX);
      showFeedback('success', nextActive ? 'User enabled' : 'User disabled');
      await loadUsers();

      if (selectedUser?.id === userId) {
        setSelectedUser((current) => (
          current ? { ...current, isActive: nextActive } : current
        ));
      }
    } catch (error) {
      showFeedback('error', getErrorMessage(error, 'Failed to update user status'));
    }
  };

  const openActionMenu = (event: React.MouseEvent<HTMLButtonElement>, userId: string) => {
    setActionMenu({
      anchorEl: event.currentTarget,
      userId,
    });
  };

  const closeActionMenu = () => {
    setActionMenu({
      anchorEl: null,
      userId: null,
    });
  };

  const handleSort = (nextSortKey: SortKey) => {
    if (sortKey === nextSortKey) {
      setSortDirection((current) => (current === 'asc' ? 'desc' : 'asc'));
      return;
    }

    setSortKey(nextSortKey);
    setSortDirection('asc');
  };

  const renderSortHeader = (label: string, columnKey: SortKey) => (
    <TableSortLabel
      active={sortKey === columnKey}
      direction={sortKey === columnKey ? sortDirection : 'asc'}
      onClick={() => handleSort(columnKey)}
    >
      {label}
    </TableSortLabel>
  );

  const toolbarFieldSx = {
    minWidth: 180,
    '& .MuiOutlinedInput-root': {
      height: 36,
      borderRadius: 1,
      fontSize: 14,
    },
    '& .MuiOutlinedInput-input': {
      py: 0,
      px: 1.25,
      display: 'flex',
      alignItems: 'center',
    },
  } as const;

  return (
    <AuthGuard requiredScopes={['admin.users', 'admin.full']}>
      <AppShell
        title="Users"
        subtitle="User management and access operations."
      >
        <Stack spacing={3} data-testid="users-management-page">
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
                  List
                </Typography>
              </Breadcrumbs>
            </Stack>

            <CreateUserDialog
              availableRoles={availableRoles}
              onCreate={handleCreateUser}
            />
          </Stack>

          <SurfacePanel>
            <Tabs
              value={statusFilter}
              onChange={(_, value: StatusFilter) => {
                setStatusFilter(value);
                setPageNumber(1);
              }}
              sx={{
                px: 2.5,
                pt: 1,
                minHeight: 56,
                '& .MuiTab-root': {
                  minHeight: 48,
                  textTransform: 'none',
                  fontWeight: 600,
                  alignItems: 'center',
                  gap: 1,
                },
              }}
            >
              <Tab
                value="all"
                label={(
                  <Stack direction="row" spacing={1} alignItems="center">
                    <span>All</span>
                    <AppLabel>{totalUsers}</AppLabel>
                  </Stack>
                )}
              />
              <Tab
                value="active"
                label={(
                  <Stack direction="row" spacing={1} alignItems="center">
                    <span>Active</span>
                    <AppLabel color="success">{activeUsersOnPage}</AppLabel>
                  </Stack>
                )}
              />
              <Tab
                value="inactive"
                label={(
                  <Stack direction="row" spacing={1} alignItems="center">
                    <span>Disabled</span>
                    <AppLabel color="warning">{inactiveUsersOnPage}</AppLabel>
                  </Stack>
                )}
              />
            </Tabs>

            <Divider />

            <Stack
              direction={{ xs: 'column', lg: 'row' }}
              spacing={2}
              justifyContent="space-between"
              alignItems={{ xs: 'stretch', lg: 'center' }}
              sx={{ p: 2.5 }}
            >
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems={{ sm: 'center' }}>
                <TextField
                  select
                  size="small"
                  value={roleFilter}
                  onChange={(event) => {
                    setRoleFilter(event.target.value);
                    setPageNumber(1);
                  }}
                  disabled={isLoadingRoles && availableRoles.length === 0}
                  sx={toolbarFieldSx}
                  inputProps={{ 'data-testid': 'users-role-filter' }}
                  SelectProps={{
                    displayEmpty: true,
                    renderValue: (selected) => {
                      const selectedValue = typeof selected === 'string' ? selected : '';

                      if (!selectedValue) {
                        return (
                          <Box component="span" sx={{ color: 'text.secondary' }}>
                            Role
                          </Box>
                        );
                      }

                      return selectedValue;
                    },
                  }}
                >
                  <MenuItem value="">All roles</MenuItem>
                  {availableRoles.map((role) => (
                    <MenuItem key={role.id} value={role.name}>
                      {role.name}
                    </MenuItem>
                  ))}
                </TextField>

                {isLoadingSelectedUser ? (
                  <Stack direction="row" spacing={1} alignItems="center">
                    <CircularProgress size={16} />
                    <Typography variant="caption" color="text.secondary">
                      Loading selected user...
                    </Typography>
                  </Stack>
                ) : null}

                {isRefetchingUsers ? (
                  <Stack direction="row" spacing={1} alignItems="center">
                    <CircularProgress size={16} />
                    <Typography variant="caption" color="text.secondary">
                      {showSlowUsersHint ? 'Still loading users...' : 'Updating list...'}
                    </Typography>
                  </Stack>
                ) : null}
              </Stack>

              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} alignItems={{ sm: 'center' }}>
                <OutlinedInput
                  size="small"
                  value={searchInput}
                  placeholder="Search..."
                  onChange={(event) => setSearchInput(event.target.value)}
                  onKeyDown={(event) => {
                    if (event.key === 'Enter') {
                      handleApplyFilters();
                    }
                  }}
                  startAdornment={(
                    <InputAdornment position="start">
                      <Icon icon="eva:search-fill" width={16} />
                    </InputAdornment>
                  )}
                  inputProps={{ 'data-testid': 'users-search-input' }}
                  sx={{
                    minWidth: { xs: '100%', sm: 280 },
                    '&.MuiOutlinedInput-root': {
                      height: 36,
                      borderRadius: 1,
                      fontSize: 14,
                    },
                  }}
                />

                <Button
                  size="small"
                  variant="outlined"
                  onClick={handleClearFilters}
                  sx={{ height: 36, borderRadius: 1, px: 1.5 }}
                >
                  Clear
                </Button>

                <IconButton
                  size="small"
                  onClick={handleApplyFilters}
                  aria-label="Apply filters"
                  sx={{
                    width: 36,
                    height: 36,
                    borderRadius: 1,
                    border: '1px solid',
                    borderColor: 'divider',
                    color: 'text.secondary',
                  }}
                >
                  <Icon icon="ic:round-filter-list" width={18} />
                </IconButton>
              </Stack>
            </Stack>

            {usersLoadError ? (
              <Box sx={{ px: 2.5, pb: 1.5 }}>
                <Alert
                  severity="error"
                  variant="outlined"
                  action={(
                    <Button color="inherit" size="small" onClick={() => void loadUsers()}>
                      Retry
                    </Button>
                  )}
                >
                  {usersLoadError}
                </Alert>
              </Box>
            ) : null}

            {rolesLoadError ? (
              <Box sx={{ px: 2.5, pb: 1.5 }}>
                <Alert
                  severity="warning"
                  variant="outlined"
                  action={(
                    <Button color="inherit" size="small" onClick={() => void loadRoles()}>
                      Retry
                    </Button>
                  )}
                >
                  {rolesLoadError}
                </Alert>
              </Box>
            ) : null}

            {isRefetchingUsers ? <LinearProgress sx={{ mx: 2.5, mb: 0.5 }} /> : null}

            {isInitialLoadingUsers ? (
              <Stack
                spacing={1.5}
                alignItems="center"
                justifyContent="center"
                sx={{ py: 8 }}
              >
                <CircularProgress size={28} />
                <Typography color="text.secondary">Loading users...</Typography>
                {showSlowUsersHint ? (
                  <Typography variant="caption" color="text.secondary">
                    This is taking longer than expected.
                  </Typography>
                ) : null}
              </Stack>
            ) : sortedUsers.length > 0 ? (
              <>
                <TableContainer>
                  <Table
                    sx={{
                      minWidth: 860,
                      '& .MuiTableCell-root': {
                        py: dense ? 1 : 1.75,
                      },
                    }}
                    data-testid="users-table"
                  >
                    <TableHead>
                      <TableRow>
                        <TableCell>{renderSortHeader('Name', 'email')}</TableCell>
                        <TableCell>{renderSortHeader('Phone number', 'phone')}</TableCell>
                        <TableCell>{renderSortHeader('Role', 'roles')}</TableCell>
                        <TableCell>{renderSortHeader('Status', 'status')}</TableCell>
                        <TableCell>{renderSortHeader('Created', 'createdAt')}</TableCell>
                        <TableCell align="right" />
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {sortedUsers.map((entry) => (
                        <TableRow key={entry.id} hover>
                          <TableCell>
                            <Stack spacing={0.5}>
                              <Typography
                                component={Link}
                                href="#"
                                variant="body2"
                                fontWeight={700}
                                sx={{
                                  color: 'text.primary',
                                  textDecoration: 'none',
                                  '&:hover': {
                                    color: 'primary.main',
                                  },
                                }}
                              >
                                {entry.email}
                              </Typography>
                              <Typography variant="caption" color="text.secondary">
                                {entry.userName}
                              </Typography>
                            </Stack>
                          </TableCell>
                          <TableCell>{entry.phoneNumber || '-'}</TableCell>
                          <TableCell>
                            <Stack direction="row" spacing={0.75} useFlexGap flexWrap="wrap">
                              {entry.roles.length > 0 ? entry.roles.map((roleName) => (
                                <AppLabel key={`${entry.id}-${roleName}`}>
                                  {roleName}
                                </AppLabel>
                              )) : (
                                <Typography variant="body2" color="text.secondary">
                                  No roles
                                </Typography>
                              )}
                            </Stack>
                          </TableCell>
                          <TableCell>
                            <AppLabel color={entry.isActive ? 'success' : 'warning'}>
                              {entry.isActive ? 'Active' : 'Disabled'}
                            </AppLabel>
                          </TableCell>
                          <TableCell>
                            {new Date(entry.createdAt).toLocaleDateString()}
                          </TableCell>
                          <TableCell align="right">
                            <Stack direction="row" spacing={0.5} justifyContent="flex-end">
                              <IconButton
                                size="small"
                                aria-label={`Quick edit ${entry.email}`}
                                onClick={() => void openEditDialog(entry.id)}
                              >
                                <Icon icon="solar:pen-bold" width={18} />
                              </IconButton>
                              <IconButton
                                size="small"
                                onClick={(event) => openActionMenu(event, entry.id)}
                                aria-label={`Open actions for ${entry.email}`}
                                data-testid={`user-actions-${entry.id}`}
                              >
                                <Icon icon="eva:more-vertical-fill" width={18} />
                              </IconButton>
                            </Stack>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>

                <Divider />

                <Stack
                  direction={{ xs: 'column', md: 'row' }}
                  justifyContent="space-between"
                  alignItems={{ xs: 'stretch', md: 'center' }}
                  spacing={1.5}
                  sx={{ px: 1.5, py: 1 }}
                >
                  <TablePagination
                    component="div"
                    count={usersResponse?.totalCount ?? 0}
                    page={Math.max((usersResponse?.pageNumber ?? 1) - 1, 0)}
                    rowsPerPage={rowsPerPage}
                    onPageChange={(_, nextPage) => setPageNumber(nextPage + 1)}
                    rowsPerPageOptions={[5, 10, 20]}
                    onRowsPerPageChange={(event) => {
                      setRowsPerPage(Number(event.target.value));
                      setPageNumber(1);
                    }}
                  />

                  <FormControlLabel
                    control={(
                      <Switch
                        size="small"
                        checked={dense}
                        onChange={(_, checked) => setDense(checked)}
                      />
                    )}
                    label="Dense"
                    sx={{ mr: 1.5 }}
                  />
                </Stack>
              </>
            ) : (
              <Box sx={{ p: 2.5 }}>
                <EmptyStateCard
                  eyebrow="No matches"
                  title="No users found for the current filters"
                  description="Adjust the search, status, or role filters, or create a new user to populate the table."
                />
              </Box>
            )}
          </SurfacePanel>
        </Stack>

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

        <Menu
          open={Boolean(actionMenu.anchorEl)}
          anchorEl={actionMenu.anchorEl}
          onClose={closeActionMenu}
          anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
          transformOrigin={{ vertical: 'top', horizontal: 'right' }}
          slotProps={{
            paper: {
              sx: {
                mt: 1,
                minWidth: 180,
                p: 0.5,
                borderRadius: 2,
              },
            },
          }}
        >
          <MenuItem
            onClick={() => {
              if (selectedMenuUser) {
                closeActionMenu();
                void openRolesDialog(selectedMenuUser.id);
              }
            }}
          >
            Roles
          </MenuItem>
          <MenuItem
            onClick={() => {
              if (selectedMenuUser) {
                closeActionMenu();
                void openResetPasswordDialog(selectedMenuUser.id);
              }
            }}
          >
            Reset password
          </MenuItem>
          <MenuItem
            disabled={Boolean(selectedMenuUser && currentUserId === selectedMenuUser.id && selectedMenuUser.isActive)}
            data-testid={selectedMenuUser ? `toggle-user-status-${selectedMenuUser.id}` : undefined}
            onClick={() => {
              if (selectedMenuUser) {
                closeActionMenu();
                void handleToggleStatus(selectedMenuUser.id, selectedMenuUser.isActive);
              }
            }}
            sx={{
              color: selectedMenuUser?.isActive ? 'error.main' : 'success.main',
            }}
          >
            {selectedMenuUser?.isActive ? 'Disable' : 'Enable'}
          </MenuItem>
        </Menu>

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
