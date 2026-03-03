import type { ReactNode } from 'react';

import Paper from '@mui/material/Paper';
import type { SxProps, Theme } from '@mui/material/styles';

type SurfacePanelProps = {
  children: ReactNode;
  sx?: SxProps<Theme>;
};

export function SurfacePanel({ children, sx }: SurfacePanelProps) {
  return (
    <Paper
      elevation={0}
      sx={[
        {
          border: 1,
          borderColor: 'divider',
          bgcolor: 'background.paper',
        },
        ...(Array.isArray(sx) ? sx : [sx]),
      ]}
    >
      {children}
    </Paper>
  );
}
