import Link from 'next/link';

import Button from '@mui/material/Button';
import Stack from '@mui/material/Stack';

import { AppShell } from '@/components/app-shell';
import { DashboardCard } from '@/components/common/dashboard-card';
import { EmptyStateCard } from '@/components/common/empty-state-card';

export default function OrdersPage() {
  return (
    <AppShell
      title="Orders"
      subtitle="Track order flow, status, and fulfillment from the new workspace."
    >
      <Stack spacing={3}>
        <DashboardCard
          title="Orders workspace"
          description="The full order list is the next business page to move into this new shell."
        >
          <EmptyStateCard
            eyebrow="Coming soon"
            title="The full orders list is not in this app yet"
            description="Filters, refresh controls, and the full operational list are being moved into the new interface next. The route is stable now so navigation and layout can be validated first."
            action={
              <Button component={Link} href="/orders/new" variant="contained">
                Open new order
              </Button>
            }
          />
        </DashboardCard>
      </Stack>
    </AppShell>
  );
}
