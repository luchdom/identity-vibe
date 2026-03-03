import type { ReactNode } from 'react';

import { DashboardCard } from './dashboard-card';

type TableCardShellProps = {
  title: string;
  description?: string;
  action?: ReactNode;
  children: ReactNode;
};

export function TableCardShell({
  title,
  description,
  action,
  children,
}: TableCardShellProps) {
  return (
    <DashboardCard
      title={title}
      description={description}
      action={action}
      contentSx={{ overflowX: 'auto' }}
    >
      {children}
    </DashboardCard>
  );
}
