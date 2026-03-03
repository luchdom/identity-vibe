import type { ReactNode } from 'react';

import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import { SurfacePanel } from './surface-panel';

type EmptyStateCardProps = {
  eyebrow?: string;
  title: string;
  description: string;
  action?: ReactNode;
};

export function EmptyStateCard({
  eyebrow,
  title,
  description,
  action,
}: EmptyStateCardProps) {
  return (
    <SurfacePanel>
      <Stack
        spacing={2}
        sx={{
          p: { xs: 2.5, sm: 3 },
          textAlign: 'center',
          alignItems: 'center',
        }}
      >
        {eyebrow ? (
          <Typography variant="overline" color="primary.main" fontWeight={700}>
            {eyebrow}
          </Typography>
        ) : null}
        <Typography variant="h6">{title}</Typography>
        <Typography color="text.secondary">
          {description}
        </Typography>
        {action}
      </Stack>
    </SurfacePanel>
  );
}
