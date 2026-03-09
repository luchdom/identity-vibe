'use client';

import { useCallback, useEffect, useMemo, useRef, useState, useTransition } from 'react';
import { usePathname, useRouter, useSearchParams } from 'next/navigation';

import { Icon } from '@iconify/react';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import IconButton from '@mui/material/IconButton';
import LinearProgress from '@mui/material/LinearProgress';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import Snackbar from '@mui/material/Snackbar';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TablePagination from '@mui/material/TablePagination';
import TableRow from '@mui/material/TableRow';
import TextField from '@mui/material/TextField';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';

import { AppShell } from '@/components/app-shell';
import { EmptyStateCard } from '@/components/common/empty-state-card';
import { SurfacePanel } from '@/components/common/surface-panel';
import { OrderStatusLabel } from '@/components/orders';
import { useAuth } from '@/contexts/auth-context';
import { useNavigationProgress } from '@/contexts/navigation-progress-context';
import {
  ORDERS_LIST_CACHE_PREFIX,
  orderDetailCacheKey,
  ordersListCacheKey,
} from '@/lib/cache/query-keys';
import { queryCache } from '@/lib/cache/query-cache';
import { isRequestAborted } from '@/lib/api';
import { useDelayedFlag } from '@/hooks/use-delayed-flag';
import { ordersApi } from '@/lib/orders-api';
import { canCreateOrder, canViewAllOrders } from '@/lib/permissions/authz';
import { OrderStatus, type OrderListResponse } from '@/types/orders';

type FeedbackState = {
  open: boolean;
  severity: 'success' | 'error';
  message: string;
};

type ActionMenuState = {
  anchorEl: HTMLElement | null;
  orderId: number | null;
};

const STATUS_FILTERS: Array<{ label: string; value: OrderStatus | null }> = [
  { label: 'All statuses', value: null },
  { label: 'Draft', value: OrderStatus.Draft },
  { label: 'Confirmed', value: OrderStatus.Confirmed },
  { label: 'Processing', value: OrderStatus.Processing },
  { label: 'Shipped', value: OrderStatus.Shipped },
  { label: 'Delivered', value: OrderStatus.Delivered },
  { label: 'Cancelled', value: OrderStatus.Cancelled },
];

function formatMoney(value: number, currency = 'USD') {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
    maximumFractionDigits: 2,
  }).format(value);
}

function getErrorMessage(error: unknown, fallback: string): string {
  if (typeof error === 'object' && error !== null) {
    const maybeAxios = error as { response?: { data?: { detail?: string; message?: string } }; message?: string };
    return maybeAxios.response?.data?.detail || maybeAxios.response?.data?.message || maybeAxios.message || fallback;
  }

  return fallback;
}

function parsePositiveInt(value: string | null, fallback: number) {
  const parsed = Number(value);
  if (!Number.isFinite(parsed) || parsed < 1) {
    return fallback;
  }

  return Math.floor(parsed);
}

export default function OrdersPage() {
  const ordersAbortRef = useRef<AbortController | null>(null);
  const ordersRequestIdRef = useRef(0);
  const prefetchedRoutesRef = useRef<Set<string>>(new Set());
  const hoverPrefetchTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const { scopes } = useAuth();
  const { startNavigation } = useNavigationProgress();
  const searchParams = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();
  const [isRoutePending, startRouteTransition] = useTransition();

  const [data, setData] = useState<OrderListResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<FeedbackState>({
    open: false,
    severity: 'success',
    message: '',
  });
  const [actionMenu, setActionMenu] = useState<ActionMenuState>({
    anchorEl: null,
    orderId: null,
  });

  const selectedStatusParam = searchParams.get('status');
  const selectedStatus = selectedStatusParam ? Number(selectedStatusParam) as OrderStatus : undefined;
  const page = parsePositiveInt(searchParams.get('page'), 1);
  const rowsPerPage = parsePositiveInt(searchParams.get('pageSize'), 10);

  const canCreate = canCreateOrder(scopes);
  const canViewAll = canViewAllOrders(scopes);
  const orderScope: 'all' | 'mine' = canViewAll ? 'all' : 'mine';
  const isInitialLoading = isLoading && !data;
  const isRefetching = isLoading && Boolean(data);
  const showSlowOrdersHint = useDelayedFlag(isLoading, 1200);
  const hasActiveFilters = selectedStatus !== undefined;

  const selectedOrder = useMemo(
    () => data?.orders.find((entry) => entry.id === actionMenu.orderId) ?? null,
    [actionMenu.orderId, data?.orders]
  );

  const showFeedback = useCallback((severity: FeedbackState['severity'], message: string) => {
    setFeedback({
      open: true,
      severity,
      message,
    });
  }, []);

  const updateQueryParams = useCallback((mutate: (params: URLSearchParams) => void) => {
    if (isRoutePending) {
      return;
    }

    const nextParams = new URLSearchParams(searchParams.toString());
    mutate(nextParams);
    const nextQuery = nextParams.toString();
    const nextUrl = nextQuery ? `${pathname}?${nextQuery}` : pathname;
    const currentQuery = searchParams.toString();
    const currentUrl = currentQuery ? `${pathname}?${currentQuery}` : pathname;

    if (nextUrl === currentUrl) {
      return;
    }

    startNavigation();
    startRouteTransition(() => {
      router.push(nextUrl);
    });
  }, [isRoutePending, pathname, router, searchParams, startNavigation]);

  const navigateTo = useCallback((targetUrl: string) => {
    if (isRoutePending) {
      return;
    }

    startNavigation();
    startRouteTransition(() => {
      router.push(targetUrl);
    });
  }, [isRoutePending, router, startNavigation]);

  const prefetchRoute = useCallback((targetUrl: string) => {
    if (prefetchedRoutesRef.current.has(targetUrl)) {
      return;
    }

    prefetchedRoutesRef.current.add(targetUrl);
    router.prefetch(targetUrl);
  }, [router]);

  const scheduleOrderDetailPrefetch = useCallback((orderId: number) => {
    if (hoverPrefetchTimeoutRef.current) {
      clearTimeout(hoverPrefetchTimeoutRef.current);
    }

    hoverPrefetchTimeoutRef.current = setTimeout(() => {
      prefetchRoute(`/orders/${orderId}`);
      hoverPrefetchTimeoutRef.current = null;
    }, 140);
  }, [prefetchRoute]);

  const loadOrders = useCallback(async () => {
    const requestId = ++ordersRequestIdRef.current;
    ordersAbortRef.current?.abort();
    const controller = new AbortController();
    ordersAbortRef.current = controller;

    const listKey = ordersListCacheKey({
      scope: orderScope,
      status: selectedStatus,
      page,
      pageSize: rowsPerPage,
    });
    const cached = queryCache.get<OrderListResponse>(listKey);
    if (cached) {
      setData(cached);
    }

    setIsLoading(true);
    setLoadError(null);
    try {
      const response = canViewAll
        ? await ordersApi.getAllOrders({ page, pageSize: rowsPerPage, status: selectedStatus }, controller.signal)
        : await ordersApi.getOrders({ page, pageSize: rowsPerPage, status: selectedStatus }, controller.signal);
      if (requestId !== ordersRequestIdRef.current) {
        return;
      }

      setData(response);
      queryCache.set(listKey, response);
    } catch (error) {
      if (isRequestAborted(error) || requestId !== ordersRequestIdRef.current) {
        return;
      }

      const message = getErrorMessage(error, 'Failed to load orders');
      setLoadError(message);
      showFeedback('error', message);
    } finally {
      if (requestId === ordersRequestIdRef.current) {
        setIsLoading(false);
      }
    }
  }, [canViewAll, orderScope, page, rowsPerPage, selectedStatus, showFeedback]);

  useEffect(() => {
    void loadOrders();
  }, [loadOrders]);

  useEffect(() => () => {
    ordersAbortRef.current?.abort();

    if (hoverPrefetchTimeoutRef.current) {
      clearTimeout(hoverPrefetchTimeoutRef.current);
    }
  }, []);

  useEffect(() => {
    if (searchParams.get('created') !== '1') {
      return;
    }

    showFeedback('success', 'Order created successfully');
    updateQueryParams((params) => {
      params.delete('created');
    });
  }, [searchParams, showFeedback, updateQueryParams]);

  const handleStatusFilterChange = (nextStatus?: OrderStatus) => {
    updateQueryParams((params) => {
      if (nextStatus === undefined) {
        params.delete('status');
      } else {
        params.set('status', String(nextStatus));
      }
      params.set('page', '1');
    });
  };

  const handleClearFilters = () => {
    updateQueryParams((params) => {
      params.delete('status');
      params.set('page', '1');
    });
  };

  const handleCancelOrder = async (orderId: number) => {
    const confirmed = window.confirm('Cancel this order?');
    if (!confirmed) {
      return;
    }

    try {
      const response = await ordersApi.cancelOrder(orderId, 'Cancelled by admin');
      queryCache.invalidatePrefix(ORDERS_LIST_CACHE_PREFIX);
      queryCache.invalidate(orderDetailCacheKey(orderId));
      queryCache.set(orderDetailCacheKey(orderId), response.order);
      showFeedback('success', 'Order cancelled');
      await loadOrders();
    } catch (error) {
      showFeedback('error', getErrorMessage(error, 'Failed to cancel order'));
    }
  };

  return (
    <AppShell
      title="Orders"
      subtitle="Track order flow, status, and fulfillment."
    >
      <Stack spacing={2.5}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          justifyContent="space-between"
          alignItems={{ xs: 'flex-start', sm: 'center' }}
          spacing={1.25}
        >
          <Typography variant="h4">
            Orders {data ? <Typography component="span" variant="body2" color="text.secondary">({data.totalCount})</Typography> : null}
          </Typography>
          <Stack direction="row" spacing={1} alignItems="center">
            {canCreate ? (
              <Button
                size="small"
                variant="contained"
                color="inherit"
                startIcon={<Icon icon="mingcute:add-line" width={16} />}
                onMouseEnter={() => prefetchRoute('/orders/new')}
                onFocus={() => prefetchRoute('/orders/new')}
                onClick={() => navigateTo('/orders/new')}
                disabled={isRoutePending}
                data-testid="new-order-button"
                sx={{ height: 36, borderRadius: 1, px: 1.5 }}
              >
                New order
              </Button>
            ) : (
              <Tooltip title="Requires orders.write or orders.manage">
                <span>
                  <Button
                    size="small"
                    variant="contained"
                    color="inherit"
                    disabled
                    data-testid="new-order-button"
                    sx={{ height: 36, borderRadius: 1, px: 1.5 }}
                  >
                    New order
                  </Button>
                </span>
              </Tooltip>
            )}
          </Stack>
        </Stack>

        <SurfacePanel>
          <Stack
            direction={{ xs: 'column', lg: 'row' }}
            spacing={2}
            justifyContent="space-between"
            alignItems={{ xs: 'stretch', lg: 'center' }}
            sx={{ p: 2.5 }}
            data-testid="orders-table-toolbar"
          >
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems={{ sm: 'center' }}>
              <TextField
                select
                size="small"
                value={selectedStatus ?? ''}
                onChange={(event) => {
                  const nextValue = event.target.value;
                  handleStatusFilterChange(nextValue === '' ? undefined : Number(nextValue) as OrderStatus);
                }}
                sx={{
                  minWidth: 200,
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
                }}
                SelectProps={{
                  displayEmpty: true,
                  SelectDisplayProps: {
                    'data-testid': 'orders-status-filter-trigger',
                  },
                  renderValue: (selected) => {
                    if (selected === '') {
                      return (
                        <Box component="span" sx={{ color: 'text.secondary' }}>
                          Status
                        </Box>
                      );
                    }

                    return STATUS_FILTERS.find((filter) => String(filter.value ?? '') === String(selected))?.label ?? 'Status';
                  },
                  inputProps: {
                    'aria-label': 'Filter orders by status',
                    'data-testid': 'orders-status-filter',
                  },
                }}
              >
                {STATUS_FILTERS.map((filter) => (
                  <MenuItem key={filter.label} value={filter.value ?? ''}>
                    {filter.label}
                  </MenuItem>
                ))}
              </TextField>
            </Stack>

            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} alignItems={{ sm: 'center' }}>
              <Button
                size="small"
                variant="outlined"
                onClick={handleClearFilters}
                disabled={!hasActiveFilters || isRoutePending}
                data-testid="orders-clear-filters"
                sx={{ height: 36, borderRadius: 1, px: 1.5 }}
              >
                Clear
              </Button>
              <Button
                size="small"
                variant="outlined"
                onClick={() => void loadOrders()}
                disabled={isLoading}
                data-testid="orders-refresh-button"
                sx={{ height: 36, borderRadius: 1, px: 1.5 }}
              >
                {isRefetching ? 'Refreshing...' : 'Refresh'}
              </Button>
            </Stack>
          </Stack>

          {loadError ? (
            <Box sx={{ px: 2.5, pb: 1 }}>
              <Alert
                severity="error"
                variant="outlined"
                action={(
                  <Button size="small" color="inherit" onClick={() => void loadOrders()}>
                    Retry
                  </Button>
                )}
              >
                {loadError}
              </Alert>
            </Box>
          ) : null}

          {isRefetching ? (
            <Stack
              spacing={0.75}
              sx={{
                mx: 2.5,
                mb: 0.5,
              }}
            >
              <LinearProgress />
              {showSlowOrdersHint ? (
                <Typography variant="caption" color="text.secondary">
                  Updating orders is taking longer than expected.
                </Typography>
              ) : null}
            </Stack>
          ) : null}

          {isInitialLoading ? (
            <Stack spacing={1.5} alignItems="center" justifyContent="center" sx={{ py: 8 }}>
              <CircularProgress size={28} />
              <Typography color="text.secondary">Loading orders...</Typography>
              {showSlowOrdersHint ? (
                <Typography variant="caption" color="text.secondary">
                  This is taking longer than expected.
                </Typography>
              ) : null}
            </Stack>
          ) : data ? (
            <>
              {data.orders.length > 0 ? (
                <>
                  <TableContainer>
                    <Table sx={{ minWidth: 980 }}>
                      <TableHead>
                        <TableRow>
                          <TableCell>Order</TableCell>
                          <TableCell>Customer</TableCell>
                          <TableCell>Status</TableCell>
                          <TableCell align="right">Total</TableCell>
                          <TableCell align="right">Items</TableCell>
                          <TableCell>Created</TableCell>
                          <TableCell align="right" />
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {data.orders.map((order) => (
                          <TableRow
                            key={order.id}
                            hover
                            onMouseEnter={() => scheduleOrderDetailPrefetch(order.id)}
                            onMouseLeave={() => {
                              if (hoverPrefetchTimeoutRef.current) {
                                clearTimeout(hoverPrefetchTimeoutRef.current);
                                hoverPrefetchTimeoutRef.current = null;
                              }
                            }}
                          >
                            <TableCell>
                              <Typography variant="body2" fontWeight={700}>
                                #{order.orderNumber}
                              </Typography>
                            </TableCell>
                            <TableCell>
                              <Stack spacing={0.25}>
                                <Typography variant="body2" fontWeight={600}>
                                  {order.customerName}
                                </Typography>
                                <Typography variant="caption" color="text.secondary">
                                  {order.customerEmail}
                                </Typography>
                              </Stack>
                            </TableCell>
                            <TableCell>
                              <OrderStatusLabel status={order.status} statusDisplay={order.statusDisplay} />
                            </TableCell>
                            <TableCell align="right">{formatMoney(order.totalAmount, order.currency)}</TableCell>
                            <TableCell align="right">{order.itemCount}</TableCell>
                            <TableCell>{new Date(order.createdAt).toLocaleDateString()}</TableCell>
                            <TableCell align="right">
                              <IconButton
                                size="small"
                                aria-label={`Open actions for order ${order.orderNumber}`}
                                onClick={(event) => {
                                  setActionMenu({
                                    anchorEl: event.currentTarget,
                                    orderId: order.id,
                                  });
                                }}
                              >
                                <Icon icon="eva:more-vertical-fill" width={18} />
                              </IconButton>
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>

                  <TablePagination
                    component="div"
                    count={data.totalCount}
                    page={Math.max(data.pageNumber - 1, 0)}
                    rowsPerPage={rowsPerPage}
                    rowsPerPageOptions={[5, 10, 20]}
                    onPageChange={(_, nextPage) => {
                      updateQueryParams((params) => {
                        params.set('page', String(nextPage + 1));
                      });
                    }}
                    onRowsPerPageChange={(event) => {
                      updateQueryParams((params) => {
                        params.set('pageSize', event.target.value);
                        params.set('page', '1');
                      });
                    }}
                  />
                </>
              ) : (
                <Box sx={{ p: 2.5 }}>
                  <EmptyStateCard
                    eyebrow="No matches"
                    title="No orders found"
                    description="Adjust the status filter, clear the filter, or create a new order."
                  />
                </Box>
              )}
            </>
          ) : (
            <Box sx={{ p: 2.5 }}>
              <Alert severity="error" variant="outlined">
                Orders data is unavailable. Retry to load.
              </Alert>
            </Box>
          )}
        </SurfacePanel>
      </Stack>

      <Menu
        open={Boolean(actionMenu.anchorEl)}
        anchorEl={actionMenu.anchorEl}
        onClose={() => setActionMenu({ anchorEl: null, orderId: null })}
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
          disabled={!selectedOrder || isRoutePending}
          onClick={() => {
            if (selectedOrder) {
              setActionMenu({ anchorEl: null, orderId: null });
              navigateTo(`/orders/${selectedOrder.id}`);
            }
          }}
        >
          View details
        </MenuItem>
        <MenuItem
          disabled={!selectedOrder || !canCreate || !selectedOrder.canBeCancelled}
          onClick={() => {
            if (selectedOrder) {
              setActionMenu({ anchorEl: null, orderId: null });
              void handleCancelOrder(selectedOrder.id);
            }
          }}
          sx={{ color: selectedOrder?.canBeCancelled ? 'error.main' : undefined }}
        >
          Cancel order
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
  );
}
