'use client';

import Box from '@mui/material/Box';
import Skeleton from '@mui/material/Skeleton';
import Stack from '@mui/material/Stack';

const drawerWidth = 296;

export default function DashboardLoading() {
  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', bgcolor: 'background.default' }}>
      <Box
        sx={{
          display: { xs: 'none', lg: 'block' },
          width: drawerWidth,
          flexShrink: 0,
          borderRight: '1px solid',
          borderColor: 'divider',
          bgcolor: 'background.paper',
          p: 2.5,
        }}
      >
        <Stack spacing={2}>
          <Skeleton variant="rounded" height={120} />
          <Skeleton variant="text" width={92} />
          <Stack spacing={0.75}>
            {Array.from({ length: 6 }).map((_, index) => (
              <Skeleton key={`nav-skeleton-${index}`} variant="rounded" height={44} />
            ))}
          </Stack>
        </Stack>
      </Box>

      <Box sx={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column' }}>
        <Box
          sx={{
            px: { xs: 2, sm: 3, lg: 4 },
            py: 2,
            borderBottom: '1px solid',
            borderColor: 'divider',
            bgcolor: 'background.paper',
          }}
        >
          <Stack spacing={1}>
            <Skeleton variant="text" width={180} height={36} />
            <Skeleton variant="text" width={280} height={24} />
          </Stack>
        </Box>

        <Box component="main" sx={{ flex: 1, p: { xs: 2, sm: 3, lg: 4 } }}>
          <Box sx={{ maxWidth: 1440, mx: 'auto' }}>
            <Stack spacing={2.5}>
              <Skeleton variant="rounded" height={84} />
              <Skeleton variant="rounded" height={420} />
            </Stack>
          </Box>
        </Box>
      </Box>
    </Box>
  );
}
