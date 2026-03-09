'use client';

import Link from 'next/link';

import Avatar from '@mui/material/Avatar';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Chip from '@mui/material/Chip';
import LinearProgress from '@mui/material/LinearProgress';
import List from '@mui/material/List';
import ListItem from '@mui/material/ListItem';
import ListItemAvatar from '@mui/material/ListItemAvatar';
import ListItemText from '@mui/material/ListItemText';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import Typography from '@mui/material/Typography';
import { alpha } from '@mui/material/styles';

import { Icon } from '@iconify/react';

import { AppShell } from '@/components/app-shell';
import { DashboardCard } from '@/components/common/dashboard-card';
import { MetricCard } from '@/components/common/metric-card';
import { TableCardShell } from '@/components/common/table-card-shell';

const metricCards = [
  {
    label: 'Active sessions',
    value: '128',
    delta: '+8.2%',
    deltaLabel: 'vs last week',
    icon: 'solar:users-group-rounded-bold-duotone',
  },
  {
    label: 'Orders in progress',
    value: '34',
    delta: '+4 new',
    deltaLabel: 'awaiting action',
    icon: 'solar:bag-5-bold-duotone',
  },
  {
    label: 'Admin coverage',
    value: '6 roles',
    delta: 'Stable',
    deltaLabel: 'permission model synced',
    icon: 'solar:shield-user-bold-duotone',
  },
];

const recentOrders = [
  { id: 'ORD-1049', customer: 'Northwind Trading', total: '$4,820', status: 'Processing' },
  { id: 'ORD-1048', customer: 'Acme Retail', total: '$1,240', status: 'Awaiting payment' },
  { id: 'ORD-1047', customer: 'Blue Harbor', total: '$8,910', status: 'Packed' },
  { id: 'ORD-1046', customer: 'Fieldstone Labs', total: '$2,310', status: 'Ready to ship' },
];

const activityFeed = [
  { name: 'Lucia', detail: 'Assigned two users to the Operations role', time: '12 min ago' },
  { name: 'Marco', detail: 'Reviewed a blocked order and reopened it', time: '37 min ago' },
  { name: 'Nina', detail: 'Updated role scope defaults for new operators', time: '1 hour ago' },
];

const quickActions = [
  { label: 'View Orders', href: '/orders', icon: 'solar:bag-5-bold-duotone' },
  { label: 'New Order', href: '/orders/new', icon: 'solar:add-circle-bold-duotone' },
  { label: 'Manage Users', href: '/users', icon: 'solar:user-id-bold-duotone' },
  { label: 'Manage Roles', href: '/users/roles', icon: 'solar:shield-user-bold-duotone' },
];

function getStatusTone(status: string): 'info' | 'success' | 'warning' {
  if (status === 'Processing' || status === 'Packed') {
    return 'info';
  }

  if (status === 'Ready to ship') {
    return 'success';
  }

  return 'warning';
}

export default function DashboardPage() {
  return (
    <AppShell
      title="Dashboard"
      subtitle="Operational overview and quick actions for the new admin workspace."
    >
      <Stack spacing={3}>
        <Box
          sx={{
            display: 'grid',
            gap: 3,
            gridTemplateColumns: { xs: '1fr', xl: 'minmax(0, 1.8fr) minmax(320px, 0.9fr)' },
          }}
        >
          <DashboardCard
            sx={(theme) => ({
              background: `linear-gradient(135deg, ${alpha(theme.palette.primary.main, 0.16)}, rgba(0, 167, 111, 0.06))`,
            })}
          >
            <Box
              sx={{
                display: 'grid',
                gap: 3,
                alignItems: 'center',
                gridTemplateColumns: { xs: '1fr', md: 'minmax(0, 1.2fr) minmax(220px, 0.8fr)' },
              }}
            >
              <Stack spacing={2}>
                <Chip
                  label="Welcome back"
                  color="primary"
                  sx={{ alignSelf: 'flex-start', fontWeight: 700 }}
                />
                <Stack spacing={1}>
                  <Typography variant="h3">
                    Manage orders, users, and permissions from one place
                  </Typography>
                  <Typography color="text.secondary" sx={{ maxWidth: 640 }}>
                    Review order flow, keep user access organized, and monitor
                    the core admin workspace from a cleaner dashboard surface.
                  </Typography>
                </Stack>
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
                  <Button component={Link} href="/orders" variant="contained">
                    Review orders
                  </Button>
                  <Button component={Link} href="/users" variant="outlined">
                    Open user admin
                  </Button>
                </Stack>
              </Stack>

              <Box
                sx={{
                  p: 2.5,
                  borderRadius: 4,
                  bgcolor: 'background.paper',
                  border: 1,
                  borderColor: 'divider',
                }}
              >
                <Stack spacing={2}>
                  <Stack direction="row" spacing={1} alignItems="center">
                    <Avatar sx={{ width: 40, height: 40, bgcolor: 'primary.main' }}>
                      IV
                    </Avatar>
                    <Box>
                      <Typography variant="subtitle2">Current rollout</Typography>
                      <Typography variant="body2" color="text.secondary">
                        Shell and dashboard foundation
                      </Typography>
                    </Box>
                  </Stack>

                  <Box>
                    <Stack direction="row" justifyContent="space-between" sx={{ mb: 1 }}>
                      <Typography variant="body2" color="text.secondary">
                        Visual alignment
                      </Typography>
                      <Typography variant="subtitle2">72%</Typography>
                    </Stack>
                    <LinearProgress
                      variant="determinate"
                      value={72}
                      sx={{ height: 8, borderRadius: 99 }}
                    />
                  </Box>

                  <Stack spacing={1.25}>
                    {quickActions.slice(0, 3).map((action) => (
                      <Stack
                        key={action.label}
                        direction="row"
                        spacing={1.25}
                        alignItems="center"
                      >
                        <Box
                          sx={{
                            display: 'grid',
                            placeItems: 'center',
                            width: 34,
                            height: 34,
                            borderRadius: 2.5,
                            bgcolor: 'action.hover',
                          }}
                        >
                          <Icon icon={action.icon} width={18} />
                        </Box>
                        <Typography variant="body2">{action.label}</Typography>
                      </Stack>
                    ))}
                  </Stack>
                </Stack>
              </Box>
            </Box>
          </DashboardCard>

          <DashboardCard
            title="Quick actions"
            description="High-frequency admin paths"
          >
            <Stack spacing={1.25}>
              {quickActions.map((action) => (
                <Button
                  key={action.label}
                  component={Link}
                  href={action.href}
                  variant="outlined"
                  color="inherit"
                  sx={{
                    justifyContent: 'flex-start',
                    py: 1.25,
                    borderColor: 'divider',
                  }}
                  startIcon={<Icon icon={action.icon} width={18} />}
                >
                  {action.label}
                </Button>
              ))}
            </Stack>
          </DashboardCard>
        </Box>

        <Box
          sx={{
            display: 'grid',
            gap: 3,
            gridTemplateColumns: { xs: '1fr', md: 'repeat(3, minmax(0, 1fr))' },
          }}
        >
          {metricCards.map((card) => (
            <MetricCard
              key={card.label}
              label={card.label}
              value={card.value}
              delta={card.delta}
              deltaLabel={card.deltaLabel}
              icon={<Icon icon={card.icon} width={22} />}
            />
          ))}
        </Box>

        <Box
          sx={{
            display: 'grid',
            gap: 3,
            gridTemplateColumns: { xs: '1fr', xl: 'minmax(0, 1.7fr) minmax(320px, 0.9fr)' },
          }}
        >
          <TableCardShell
            title="Recent orders"
            description="Current order flow snapshot"
            action={
              <Button component={Link} href="/orders" size="small">
                View all
              </Button>
            }
          >
            <Table size="small" sx={{ minWidth: 520 }}>
              <TableHead>
                <TableRow>
                  <TableCell>Order</TableCell>
                  <TableCell>Customer</TableCell>
                  <TableCell>Total</TableCell>
                  <TableCell>Status</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {recentOrders.map((order) => (
                  <TableRow key={order.id} hover>
                    <TableCell>
                      <Typography variant="subtitle2">{order.id}</Typography>
                    </TableCell>
                    <TableCell>{order.customer}</TableCell>
                    <TableCell>{order.total}</TableCell>
                    <TableCell>
                      <Chip
                        label={order.status}
                        color={getStatusTone(order.status)}
                        size="small"
                        variant="filled"
                      />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableCardShell>

          <Stack spacing={3}>
            <DashboardCard
              title="Team activity"
              description="Latest admin changes"
            >
              <List disablePadding>
                {activityFeed.map((item, index) => (
                  <ListItem
                    key={item.name + item.time}
                    disableGutters
                    sx={{
                      py: index === 0 ? 0 : 1.25,
                      borderTop: index === 0 ? 'none' : '1px solid',
                      borderColor: 'divider',
                    }}
                  >
                    <ListItemAvatar sx={{ minWidth: 52 }}>
                      <Avatar sx={{ bgcolor: 'primary.main' }}>
                        {item.name.charAt(0)}
                      </Avatar>
                    </ListItemAvatar>
                    <ListItemText
                      primary={item.detail}
                      secondary={item.time}
                      primaryTypographyProps={{ variant: 'body2' }}
                      secondaryTypographyProps={{
                        variant: 'caption',
                        color: 'text.secondary',
                      }}
                    />
                  </ListItem>
                ))}
              </List>
            </DashboardCard>

            <DashboardCard
              title="Workspace health"
              description="Current rollout snapshot"
            >
              <Stack spacing={2}>
                <Box>
                  <Stack direction="row" justifyContent="space-between" sx={{ mb: 1 }}>
                    <Typography variant="body2" color="text.secondary">
                      Theme and shell
                    </Typography>
                    <Typography variant="subtitle2">In progress</Typography>
                  </Stack>
                  <LinearProgress
                    variant="determinate"
                    value={78}
                    sx={{ height: 8, borderRadius: 99 }}
                  />
                </Box>

                <Box>
                  <Stack direction="row" justifyContent="space-between" sx={{ mb: 1 }}>
                    <Typography variant="body2" color="text.secondary">
                      Core feature rollout
                    </Typography>
                    <Typography variant="subtitle2">Upcoming</Typography>
                  </Stack>
                  <LinearProgress
                    variant="determinate"
                    value={28}
                    color="secondary"
                    sx={{ height: 8, borderRadius: 99 }}
                  />
                </Box>
              </Stack>
            </DashboardCard>
          </Stack>
        </Box>
      </Stack>
    </AppShell>
  );
}
