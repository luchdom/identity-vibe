export type AppNavItem = {
  title: string;
  href: string;
  icon: string;
};

export const primaryNavItems: AppNavItem[] = [
  {
    title: 'Dashboard',
    href: '/dashboard',
    icon: 'solar:chart-square-bold-duotone',
  },
  {
    title: 'Orders',
    href: '/orders',
    icon: 'solar:cart-large-2-bold-duotone',
  },
  {
    title: 'Users',
    href: '/users',
    icon: 'solar:users-group-rounded-bold-duotone',
  },
  {
    title: 'Roles & Permissions',
    href: '/users/roles',
    icon: 'solar:shield-user-bold-duotone',
  },
];
