'use client';

import { AuthGuard } from '@/components/auth/AuthGuard';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';

export default function UserActivityPage() {
  return (
    <AuthGuard requireAuth={true} requiredScopes={['admin.users', 'admin.full']}>
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0" data-testid="users-activity-page">
        <Card>
          <CardHeader>
            <CardTitle>User Activity</CardTitle>
            <CardDescription>
              Activity timeline is planned for the next iteration.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">
              Coming soon: account activity events, admin actions, and change history.
            </p>
          </CardContent>
        </Card>
      </div>
    </AuthGuard>
  );
}
