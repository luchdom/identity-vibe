export type AppNavItem = {
  title: string;
  href: string;
  icon: string;
  description: string;
};

export const primaryNavItems: AppNavItem[] = [
  {
    title: 'Dashboard',
    href: '/dashboard',
    icon: 'solar:chart-square-bold-duotone',
    description: 'Operational overview and shortcuts',
  },
  {
    title: 'Orders',
    href: '/orders',
    icon: 'solar:cart-large-2-bold-duotone',
    description: 'Orders list, detail, and creation flow',
  },
  {
    title: 'Users',
    href: '/users',
    icon: 'solar:users-group-rounded-bold-duotone',
    description: 'User management and account operations',
  },
  {
    title: 'Roles & Permissions',
    href: '/users/roles',
    icon: 'solar:shield-user-bold-duotone',
    description: 'Role catalog and scope assignments',
  },
];
