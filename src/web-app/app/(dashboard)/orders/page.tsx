'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import { usePathname, useRouter, useSearchParams } from 'next/navigation';

import { Icon } from '@iconify/react';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import IconButton from '@mui/material/IconButton';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import Snackbar from '@mui/material/Snackbar';
import Stack from '@mui/material/Stack';
import Tab from '@mui/material/Tab';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TablePagination from '@mui/material/TablePagination';
import TableRow from '@mui/material/TableRow';
import Tabs from '@mui/material/Tabs';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';

import { AppShell } from '@/components/app-shell';
import { AppLabel } from '@/components/common/app-label';
import { EmptyStateCard } from '@/components/common/empty-state-card';
import { SurfacePanel } from '@/components/common/surface-panel';
import { OrderStatusLabel } from '@/components/orders';
import { useAuth } from '@/contexts/auth-context';
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

const STATUS_FILTERS: Array<{ label: string; value?: OrderStatus }> = [
  { label: 'All', value: undefined },
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
  const { scopes } = useAuth();
  const searchParams = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();

  const [data, setData] = useState<OrderListResponse | null>(null);
  const [statusCounts, setStatusCounts] = useState<Record<string, number>>({});
  const [isLoading, setIsLoading] = useState(true);
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
    const nextParams = new URLSearchParams(searchParams.toString());
    mutate(nextParams);
    const nextQuery = nextParams.toString();
    router.push(nextQuery ? `${pathname}?${nextQuery}` : pathname);
  }, [pathname, router, searchParams]);

  const loadOrders = useCallback(async () => {
    setIsLoading(true);
    try {
      const response = canViewAll
        ? await ordersApi.getAllOrders({ page, pageSize: rowsPerPage, status: selectedStatus })
        : await ordersApi.getOrders({ page, pageSize: rowsPerPage, status: selectedStatus });
      setData(response);
    } catch (error) {
      showFeedback('error', getErrorMessage(error, 'Failed to load orders'));
    } finally {
      setIsLoading(false);
    }
  }, [canViewAll, page, rowsPerPage, selectedStatus, showFeedback]);

  const loadStatusCounts = useCallback(async () => {
    try {
      const results = await Promise.all(
        STATUS_FILTERS.map(async (filter) => {
          const response = canViewAll
            ? await ordersApi.getAllOrders({ page: 1, pageSize: 1, status: filter.value })
            : await ordersApi.getOrders({ page: 1, pageSize: 1, status: filter.value });
          return [filter.label, response.totalCount] as const;
        })
      );

      setStatusCounts(Object.fromEntries(results));
    } catch {
      setStatusCounts({});
    }
  }, [canViewAll]);

  useEffect(() => {
    void loadOrders();
  }, [loadOrders]);

  useEffect(() => {
    void loadStatusCounts();
  }, [loadStatusCounts]);

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

  const handleCancelOrder = async (orderId: number) => {
    const confirmed = window.confirm('Cancel this order?');
    if (!confirmed) {
      return;
    }

    try {
      await ordersApi.cancelOrder(orderId, 'Cancelled by admin');
      showFeedback('success', 'Order cancelled');
      await loadOrders();
      await loadStatusCounts();
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
                onClick={() => router.push('/orders/new')}
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
            <Button
              size="small"
              variant="outlined"
              onClick={() => void loadOrders()}
              sx={{ height: 36, borderRadius: 1, px: 1.5 }}
            >
              Refresh
            </Button>
          </Stack>
        </Stack>

        <SurfacePanel>
          <Tabs
            value={selectedStatus ?? 'all'}
            onChange={(_, value: OrderStatus | 'all') => {
              if (value === 'all') {
                handleStatusFilterChange(undefined);
              } else {
                handleStatusFilterChange(value);
              }
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
            {STATUS_FILTERS.map((filter) => (
              <Tab
                key={filter.label}
                value={filter.value ?? 'all'}
                label={(
                  <Stack direction="row" spacing={1} alignItems="center">
                    <span>{filter.label}</span>
                    <AppLabel>{statusCounts[filter.label] ?? 0}</AppLabel>
                  </Stack>
                )}
              />
            ))}
          </Tabs>

          {isLoading ? (
            <Stack spacing={1.5} alignItems="center" justifyContent="center" sx={{ py: 8 }}>
              <CircularProgress size={28} />
              <Typography color="text.secondary">Loading orders...</Typography>
            </Stack>
          ) : data && data.orders.length > 0 ? (
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
                      <TableRow key={order.id} hover>
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
                          <Stack direction="row" spacing={0.5} justifyContent="flex-end">
                            <IconButton
                              size="small"
                              aria-label={`Open order ${order.orderNumber}`}
                              onClick={() => router.push(`/orders/${order.id}`)}
                            >
                              <Icon icon="solar:eye-bold" width={18} />
                            </IconButton>
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
                          </Stack>
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
                eyebrow="No data"
                title="No orders found"
                description="Adjust the status filter or create a new order."
              />
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
          onClick={() => {
            if (selectedOrder) {
              setActionMenu({ anchorEl: null, orderId: null });
              router.push(`/orders/${selectedOrder.id}`);
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
