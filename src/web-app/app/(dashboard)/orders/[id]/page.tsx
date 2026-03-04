'use client';

import { useCallback, useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';

import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import Divider from '@mui/material/Divider';
import Snackbar from '@mui/material/Snackbar';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import { AppShell } from '@/components/app-shell';
import { AppLabel } from '@/components/common/app-label';
import { SurfacePanel } from '@/components/common/surface-panel';
import { OrderStatusLabel } from '@/components/orders';
import { useAuth } from '@/contexts/auth-context';
import { ordersApi } from '@/lib/orders-api';
import { canCreateOrder } from '@/lib/permissions/authz';
import type { OrderDetails } from '@/types/orders';

type FeedbackState = {
  open: boolean;
  severity: 'success' | 'error';
  message: string;
};

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

export default function OrderDetailPage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  const { scopes } = useAuth();

  const [order, setOrder] = useState<OrderDetails | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [feedback, setFeedback] = useState<FeedbackState>({
    open: false,
    severity: 'success',
    message: '',
  });

  const orderId = Number(params.id);
  const canCancelOrders = canCreateOrder(scopes);

  const loadOrder = useCallback(async () => {
    setIsLoading(true);
    try {
      const response = await ordersApi.getOrderById(orderId);
      setOrder(response);
    } catch (error) {
      setFeedback({
        open: true,
        severity: 'error',
        message: getErrorMessage(error, 'Failed to load order details'),
      });
      setOrder(null);
    } finally {
      setIsLoading(false);
    }
  }, [orderId]);

  useEffect(() => {
    if (!Number.isFinite(orderId)) {
      setFeedback({
        open: true,
        severity: 'error',
        message: 'Invalid order id',
      });
      setIsLoading(false);
      return;
    }

    void loadOrder();
  }, [loadOrder, orderId]);

  const handleCancelOrder = async () => {
    if (!order) {
      return;
    }

    const confirmed = window.confirm('Cancel this order?');
    if (!confirmed) {
      return;
    }

    try {
      await ordersApi.cancelOrder(order.id, 'Cancelled by admin');
      setFeedback({
        open: true,
        severity: 'success',
        message: 'Order cancelled',
      });
      await loadOrder();
    } catch (error) {
      setFeedback({
        open: true,
        severity: 'error',
        message: getErrorMessage(error, 'Failed to cancel order'),
      });
    }
  };

  return (
    <AppShell
      title={order ? `Order #${order.orderNumber}` : 'Order details'}
      subtitle="Order summary, customer data, and status history."
    >
      {isLoading ? (
        <Stack spacing={1.5} alignItems="center" justifyContent="center" sx={{ py: 10 }}>
          <CircularProgress size={28} />
          <Typography color="text.secondary">Loading order...</Typography>
        </Stack>
      ) : order ? (
        <Stack spacing={2.5}>
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            justifyContent="space-between"
            alignItems={{ xs: 'flex-start', sm: 'center' }}
            spacing={1.25}
          >
            <Stack spacing={0.75}>
              <Typography variant="h4">#{order.orderNumber}</Typography>
              <Typography variant="body2" color="text.secondary">
                Created {new Date(order.createdAt).toLocaleString()}
              </Typography>
            </Stack>

            <Stack direction="row" spacing={1} alignItems="center">
              <OrderStatusLabel status={order.status} statusDisplay={order.statusDisplay} />
              {order.canBeCancelled && canCancelOrders ? (
                <Button
                  size="small"
                  variant="outlined"
                  color="error"
                  onClick={() => void handleCancelOrder()}
                  sx={{ height: 36, borderRadius: 1, px: 1.5 }}
                >
                  Cancel order
                </Button>
              ) : null}
              <Button
                size="small"
                variant="outlined"
                onClick={() => router.push('/orders')}
                sx={{ height: 36, borderRadius: 1, px: 1.5 }}
              >
                Back
              </Button>
            </Stack>
          </Stack>

          <Box
            sx={{
              display: 'grid',
              gap: 2.5,
              gridTemplateColumns: { xs: '1fr', lg: '1fr 1fr' },
            }}
          >
            <SurfacePanel>
              <Stack spacing={1.25} sx={{ p: 2.5 }}>
                <Typography variant="subtitle1" fontWeight={700}>Customer</Typography>
                <Typography variant="body2">{order.customer.fullName}</Typography>
                <Typography variant="body2" color="text.secondary">{order.customer.email}</Typography>
                {order.customer.phone ? (
                  <Typography variant="body2" color="text.secondary">{order.customer.phone}</Typography>
                ) : null}
                {order.customer.address ? (
                  <Typography variant="body2" color="text.secondary">
                    {order.customer.address}
                    {order.customer.city ? `, ${order.customer.city}` : ''}
                    {order.customer.state ? `, ${order.customer.state}` : ''}
                    {order.customer.postalCode ? ` ${order.customer.postalCode}` : ''}
                    {order.customer.country ? `, ${order.customer.country}` : ''}
                  </Typography>
                ) : null}
              </Stack>
            </SurfacePanel>

            <SurfacePanel>
              <Stack spacing={1.25} sx={{ p: 2.5 }}>
                <Typography variant="subtitle1" fontWeight={700}>Summary</Typography>
                <Stack direction="row" justifyContent="space-between">
                  <Typography variant="body2" color="text.secondary">Subtotal</Typography>
                  <Typography variant="body2">{formatMoney(order.subtotalAmount, order.currency)}</Typography>
                </Stack>
                <Stack direction="row" justifyContent="space-between">
                  <Typography variant="body2" color="text.secondary">Tax</Typography>
                  <Typography variant="body2">{formatMoney(order.taxAmount, order.currency)}</Typography>
                </Stack>
                <Stack direction="row" justifyContent="space-between">
                  <Typography variant="body2" color="text.secondary">Shipping</Typography>
                  <Typography variant="body2">{formatMoney(order.shippingAmount, order.currency)}</Typography>
                </Stack>
                {order.discountAmount > 0 ? (
                  <Stack direction="row" justifyContent="space-between">
                    <Typography variant="body2" color="text.secondary">Discount</Typography>
                    <Typography variant="body2">-{formatMoney(order.discountAmount, order.currency)}</Typography>
                  </Stack>
                ) : null}
                <Divider />
                <Stack direction="row" justifyContent="space-between">
                  <Typography variant="body2" fontWeight={700}>Total</Typography>
                  <Typography variant="body2" fontWeight={700}>
                    {formatMoney(order.totalAmount, order.currency)}
                  </Typography>
                </Stack>
                {order.paymentMethod ? (
                  <Typography variant="caption" color="text.secondary">
                    Payment: {order.paymentMethod}
                  </Typography>
                ) : null}
              </Stack>
            </SurfacePanel>

            <SurfacePanel sx={{ gridColumn: { lg: '1 / -1' } }}>
              <Stack spacing={1.25} sx={{ p: 2.5 }}>
                <Typography variant="subtitle1" fontWeight={700}>
                  Items ({order.itemCount})
                </Typography>
                <Stack spacing={1}>
                  {order.items.map((item) => (
                    <Box
                      key={item.id}
                      sx={{
                        border: '1px solid',
                        borderColor: 'divider',
                        borderRadius: 1,
                        p: 1.25,
                      }}
                    >
                      <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" spacing={1}>
                        <Stack spacing={0.25}>
                          <Typography variant="body2" fontWeight={700}>
                            {item.productName}
                          </Typography>
                          {item.productSku ? (
                            <Typography variant="caption" color="text.secondary">
                              SKU: {item.productSku}
                            </Typography>
                          ) : null}
                          {item.productDescription ? (
                            <Typography variant="caption" color="text.secondary">
                              {item.productDescription}
                            </Typography>
                          ) : null}
                        </Stack>
                        <Stack alignItems={{ xs: 'flex-start', sm: 'flex-end' }}>
                          <Typography variant="body2" fontWeight={700}>
                            {formatMoney(item.lineTotal, order.currency)}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {item.quantity} × {formatMoney(item.unitPrice, order.currency)}
                          </Typography>
                        </Stack>
                      </Stack>
                    </Box>
                  ))}
                </Stack>
              </Stack>
            </SurfacePanel>

            {order.statusHistory.length > 0 ? (
              <SurfacePanel sx={{ gridColumn: { lg: '1 / -1' } }}>
                <Stack spacing={1.25} sx={{ p: 2.5 }}>
                  <Typography variant="subtitle1" fontWeight={700}>Status history</Typography>
                  <Stack spacing={1}>
                    {order.statusHistory.map((entry) => (
                      <Box
                        key={entry.id}
                        sx={{
                          border: '1px solid',
                          borderColor: 'divider',
                          borderRadius: 1,
                          p: 1.25,
                        }}
                      >
                        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" spacing={1}>
                          <Stack spacing={0.5}>
                            <OrderStatusLabel status={entry.toStatus} statusDisplay={entry.statusChange} />
                            {entry.notes || entry.reason ? (
                              <Typography variant="caption" color="text.secondary">
                                {entry.notes || entry.reason}
                              </Typography>
                            ) : null}
                          </Stack>
                          <Stack alignItems={{ xs: 'flex-start', sm: 'flex-end' }} spacing={0.25}>
                            <Typography variant="caption" color="text.secondary">
                              {new Date(entry.createdAt).toLocaleString()}
                            </Typography>
                            <AppLabel>{entry.changedByUserName || entry.changedByUserId || 'System'}</AppLabel>
                          </Stack>
                        </Stack>
                      </Box>
                    ))}
                  </Stack>
                </Stack>
              </SurfacePanel>
            ) : null}
          </Box>
        </Stack>
      ) : (
        <Alert severity="error" variant="outlined">
          Unable to load order.
        </Alert>
      )}

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
