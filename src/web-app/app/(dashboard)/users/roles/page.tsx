'use client';

import Link from 'next/link';

import Button from '@mui/material/Button';
import Stack from '@mui/material/Stack';

import { AppShell } from '@/components/app-shell';
import { AuthGuard } from '@/components/auth';
import { DashboardCard } from '@/components/common/dashboard-card';
import { EmptyStateCard } from '@/components/common/empty-state-card';

export default function RolesPermissionsPage() {
  return (
    <AuthGuard requiredScopes={['admin.roles', 'admin.full']}>
      <AppShell
        title="Roles & Permissions"
        subtitle="Role catalog and grouped scope assignment management."
      >
        <Stack spacing={3}>
          <DashboardCard
            title="Roles workspace"
            description="The final role editor will land here with the same scope-based access rules already active."
          >
            <EmptyStateCard
              eyebrow="Coming soon"
              title="The full roles editor is not in this route yet"
              description="Role metadata, grouped scope editing, create-role flows, and save/reset behavior are moving into this page after the shell and dashboard foundation."
              action={
                <Button component={Link} href="/users" variant="outlined">
                  Back to users
                </Button>
              }
            />
          </DashboardCard>
        </Stack>
      </AppShell>
    </AuthGuard>
  );
}
