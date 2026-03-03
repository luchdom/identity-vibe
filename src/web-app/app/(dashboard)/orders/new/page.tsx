'use client';

import Link from 'next/link';

import Button from '@mui/material/Button';
import Stack from '@mui/material/Stack';

import { AppShell } from '@/components/app-shell';
import { AuthGuard } from '@/components/auth';
import { DashboardCard } from '@/components/common/dashboard-card';
import { EmptyStateCard } from '@/components/common/empty-state-card';

export default function NewOrderPage() {
  return (
    <AuthGuard requiredScopes={['orders.write', 'orders.manage', 'admin.full']}>
      <AppShell
        title="New Order"
        subtitle="Create a new order with the final permission rules already enforced."
      >
        <Stack spacing={3}>
          <DashboardCard
            title="Create order"
            description="The route is protected and ready for the upcoming form migration."
          >
            <EmptyStateCard
              eyebrow="Coming soon"
              title="The new order form is being built next"
              description="Validation, submission, and redirect handling will move here. This route already uses the final create-order permission guard so access behavior stays consistent while the UI is completed."
              action={
                <Button component={Link} href="/orders" variant="outlined">
                  Back to orders
                </Button>
              }
            />
          </DashboardCard>
        </Stack>
      </AppShell>
    </AuthGuard>
  );
}
