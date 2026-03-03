import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Container from '@mui/material/Container';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import Link from 'next/link';

export default function UnauthorizedPage() {
  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'grid',
        placeItems: 'center',
        bgcolor: 'background.default',
        px: 2,
      }}
    >
      <Container maxWidth="sm">
        <Stack
          spacing={3}
          alignItems="flex-start"
          sx={{
            py: 6,
            px: { xs: 3, sm: 4 },
            border: 1,
            borderColor: 'divider',
            borderRadius: 4,
            bgcolor: 'background.paper',
          }}
        >
          <Typography
            variant="overline"
            color="error.main"
            fontWeight={700}
          >
            Access denied
          </Typography>
          <Typography variant="h4" fontWeight={700}>
            You do not have permission to view this page
          </Typography>
          <Typography color="text.secondary">
            The current account is authenticated, but it does not have the
            required scope for this route.
          </Typography>
          <Button
            component={Link}
            href="/dashboard"
            variant="contained"
          >
            Back to dashboard
          </Button>
        </Stack>
      </Container>
    </Box>
  );
}
