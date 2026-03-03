import type { ReactNode } from 'react';

import Box from '@mui/material/Box';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import { DashboardCard } from './dashboard-card';

type MetricCardProps = {
  label: string;
  value: string;
  delta: string;
  deltaLabel: string;
  icon: ReactNode;
};

export function MetricCard({
  label,
  value,
  delta,
  deltaLabel,
  icon,
}: MetricCardProps) {
  return (
    <DashboardCard>
      <Stack spacing={2.5}>
        <Stack direction="row" alignItems="flex-start" justifyContent="space-between">
          <Stack spacing={0.75}>
            <Typography variant="body2" color="text.secondary">
              {label}
            </Typography>
            <Typography variant="h4">{value}</Typography>
          </Stack>

          <Box
            sx={{
              display: 'grid',
              placeItems: 'center',
              width: 44,
              height: 44,
              borderRadius: 3,
              bgcolor: 'primary.main',
              color: 'primary.contrastText',
            }}
          >
            {icon}
          </Box>
        </Stack>

        <Stack direction="row" spacing={1} alignItems="center">
          <Typography variant="subtitle2" color="success.main">
            {delta}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {deltaLabel}
          </Typography>
        </Stack>
      </Stack>
    </DashboardCard>
  );
}
