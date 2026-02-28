'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import type { AdminRoleSummary } from '@/lib/admin-roles-api';
import { cn } from '@/lib/utils';

interface RolesListProps {
  roles: AdminRoleSummary[];
  selectedRoleId: string | null;
  isLoading?: boolean;
  onSelectRole: (roleId: string) => void;
  search: string;
  onSearchChange: (value: string) => void;
}

export function RolesList({
  roles,
  selectedRoleId,
  isLoading = false,
  onSelectRole,
  search,
  onSearchChange,
}: RolesListProps) {
  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base">Roles</CardTitle>
        <input
          data-testid="roles-search"
          className="mt-2 h-9 w-full rounded-md border px-3 text-sm"
          placeholder="Search roles..."
          value={search}
          onChange={(e) => onSearchChange(e.target.value)}
        />
      </CardHeader>
      <CardContent className="space-y-2">
        {isLoading ? (
          <div className="text-sm text-muted-foreground">Loading roles...</div>
        ) : roles.length === 0 ? (
          <div className="text-sm text-muted-foreground">No roles found.</div>
        ) : (
          roles.map((role) => {
            const isSelected = role.id === selectedRoleId;
            return (
              <button
                key={role.id}
                type="button"
                data-testid={`role-item-${role.name}`}
                className={cn(
                  'w-full rounded-lg border p-3 text-left transition-colors',
                  isSelected ? 'border-primary bg-primary/5' : 'hover:bg-muted/50'
                )}
                onClick={() => onSelectRole(role.id)}
              >
                <div className="flex items-start justify-between gap-2">
                  <div>
                    <div className="font-medium">{role.name}</div>
                    <div className="text-xs text-muted-foreground line-clamp-2">
                      {role.description || 'No description'}
                    </div>
                  </div>
                  <Badge variant={role.isActive ? 'secondary' : 'destructive'}>
                    {role.isActive ? 'Active' : 'Disabled'}
                  </Badge>
                </div>
                <div className="mt-2 flex items-center justify-between text-xs text-muted-foreground">
                  <span>{role.assignedScopeCount} permission scope(s)</span>
                  <Button type="button" variant="ghost" size="sm" className="h-6 px-2" onClick={(e) => { e.stopPropagation(); onSelectRole(role.id); }}>
                    Edit
                  </Button>
                </div>
              </button>
            );
          })
        )}
      </CardContent>
    </Card>
  );
}
