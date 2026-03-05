'use client';

import Box from '@mui/material/Box';
import Paper from '@mui/material/Paper';
import Skeleton from '@mui/material/Skeleton';
import Stack from '@mui/material/Stack';

export default function AuthLoading() {
  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'grid',
        placeItems: 'center',
        p: 2.5,
        bgcolor: 'background.default',
      }}
    >
      <Paper
        elevation={0}
        sx={{
          width: '100%',
          maxWidth: 520,
          border: '1px solid',
          borderColor: 'divider',
          borderRadius: 2,
          p: { xs: 2.5, sm: 3 },
        }}
      >
        <Stack spacing={2}>
          <Skeleton variant="rounded" width={108} height={28} />
          <Skeleton variant="text" width="72%" height={40} />
          <Skeleton variant="text" width="92%" height={24} />
          <Skeleton variant="rounded" height={46} />
          <Skeleton variant="text" width={210} height={20} />
        </Stack>
      </Paper>
    </Box>
  );
}
