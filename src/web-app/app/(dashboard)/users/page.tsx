'use client';

import Link from 'next/link';

import Button from '@mui/material/Button';
import Stack from '@mui/material/Stack';

import { AppShell } from '@/components/app-shell';
import { AuthGuard } from '@/components/auth';
import { DashboardCard } from '@/components/common/dashboard-card';
import { EmptyStateCard } from '@/components/common/empty-state-card';

export default function UsersPage() {
  return (
    <AuthGuard requiredScopes={['admin.users', 'admin.full']}>
      <AppShell
        title="Users"
        subtitle="User management, account operations, and role assignment."
      >
        <Stack spacing={3}>
          <DashboardCard
            title="User administration"
            description="The final page will keep the same scope requirements already enforced here."
          >
            <EmptyStateCard
              eyebrow="Coming soon"
              title="The full user management page is next in line"
              description="Search, create, edit, role assignment, password reset, and status controls will move into this route. Access is already gated by the final admin scope rules."
              action={
                <Button component={Link} href="/users/roles" variant="contained">
                  Open roles first
                </Button>
              }
            />
          </DashboardCard>
        </Stack>
      </AppShell>
    </AuthGuard>
  );
}
