'use client';

import type { ReactNode } from 'react';

import { AuthGuard } from '@/components/auth';

type DashboardLayoutProps = {
  children: ReactNode;
};

export default function DashboardLayout({ children }: DashboardLayoutProps) {
  return (
    <AuthGuard requireAuth>
      {children}
    </AuthGuard>
  );
}
