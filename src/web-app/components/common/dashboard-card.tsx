import type { ReactNode } from 'react';

import Box from '@mui/material/Box';
import Stack from '@mui/material/Stack';
import type { SxProps, Theme } from '@mui/material/styles';

import { SectionHeader } from './section-header';
import { SurfacePanel } from './surface-panel';

type DashboardCardProps = {
  title?: string;
  description?: string;
  action?: ReactNode;
  children: ReactNode;
  contentSx?: SxProps<Theme>;
  sx?: SxProps<Theme>;
};

export function DashboardCard({
  title,
  description,
  action,
  children,
  contentSx,
  sx,
}: DashboardCardProps) {
  return (
    <SurfacePanel sx={sx}>
      <Stack spacing={3} sx={{ p: { xs: 2.5, sm: 3 } }}>
        {title ? (
          <SectionHeader
            title={title}
            description={description}
            action={action}
          />
        ) : null}

        <Box sx={contentSx}>
          {children}
        </Box>
      </Stack>
    </SurfacePanel>
  );
}
