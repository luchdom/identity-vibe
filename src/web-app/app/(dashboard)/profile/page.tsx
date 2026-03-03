'use client';

import Button from '@mui/material/Button';
import Chip from '@mui/material/Chip';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import { AppShell } from '@/components/app-shell';
import { DashboardCard } from '@/components/common/dashboard-card';
import { useAuth } from '@/contexts/auth-context';

function formatDisplayDate(value?: string) {
  if (!value) {
    return 'Not available';
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return value;
  }

  return parsed.toLocaleDateString();
}

export default function ProfilePage() {
  const { user, scopes, logout } = useAuth();

  const displayName = [
    user?.firstName?.trim(),
    user?.lastName?.trim(),
  ]
    .filter(Boolean)
    .join(' ') || user?.email || 'Authenticated user';
  const roles = user?.roles ?? [];

  return (
    <AppShell
      title="Profile"
      subtitle="Read-only account information for the current signed-in user."
    >
      <Stack spacing={3}>
        <DashboardCard
          title="Account overview"
          description="This page reads from the current authenticated user state and granted scopes."
          action={(
            <Button
              variant="contained"
              onClick={() => {
                void logout();
              }}
            >
              Logout
            </Button>
          )}
        >
          <Stack spacing={2.5}>
            <Stack spacing={0.5}>
              <Typography variant="h4">{displayName}</Typography>
              <Typography color="text.secondary">
                {user?.email || 'No email address available'}
              </Typography>
            </Stack>

            <Stack direction={{ xs: 'column', md: 'row' }} spacing={3}>
              <Stack spacing={1} sx={{ flex: 1 }}>
                <Typography variant="subtitle2">Identity</Typography>
                <Typography variant="body2" color="text.secondary">
                  Subject: {user?.sub || user?.id || 'Not available'}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Active: {user?.isActive === false ? 'No' : 'Yes'}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Created: {formatDisplayDate(user?.createdAt)}
                </Typography>
              </Stack>

              <Stack spacing={1} sx={{ flex: 1 }}>
                <Typography variant="subtitle2">Roles</Typography>
                <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
                  {roles.length > 0 ? roles.map((role) => (
                    <Chip key={role} label={role} color="primary" variant="outlined" />
                  )) : (
                    <Typography variant="body2" color="text.secondary">
                      No role names available for this session.
                    </Typography>
                  )}
                </Stack>
              </Stack>
            </Stack>
          </Stack>
        </DashboardCard>

        <DashboardCard
          title="Granted scopes"
          description="Scope-based UI and route guards use this set."
        >
          <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
            {scopes.length > 0 ? scopes.map((scope) => (
              <Chip key={scope} label={scope} size="small" />
            )) : (
              <Typography variant="body2" color="text.secondary">
                No granted scopes are currently stored in the session.
              </Typography>
            )}
          </Stack>
        </DashboardCard>
      </Stack>
    </AppShell>
  );
}
