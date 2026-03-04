'use client';

import { useRouter } from 'next/navigation';

import Button from '@mui/material/Button';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import { AppShell } from '@/components/app-shell';
import { AuthGuard } from '@/components/auth';
import { NewOrderForm } from '@/components/orders';

export default function NewOrderPage() {
  const router = useRouter();

  return (
    <AuthGuard requiredScopes={['orders.write', 'orders.manage', 'admin.full']}>
      <AppShell
        title="New Order"
        subtitle="Create and submit a new order."
      >
        <Stack spacing={2.5}>
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            justifyContent="space-between"
            alignItems={{ xs: 'flex-start', sm: 'center' }}
            spacing={1.25}
          >
            <Typography variant="h4">Create order</Typography>
            <Button
              size="small"
              variant="outlined"
              onClick={() => router.push('/orders')}
              sx={{ height: 36, borderRadius: 1, px: 1.5 }}
            >
              Back to orders
            </Button>
          </Stack>

          <NewOrderForm
            onCancel={() => router.push('/orders')}
            onCreated={() => router.push('/orders?created=1')}
          />
        </Stack>
      </AppShell>
    </AuthGuard>
  );
}
