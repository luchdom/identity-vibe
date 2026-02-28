'use client';

import { useEffect, useMemo, useState } from 'react';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import type { AdminRoleOption, AdminUserDetail } from '@/lib/admin-users-api';

interface UserRolesDialogProps {
  user: AdminUserDetail | null;
  availableRoles: AdminRoleOption[];
  currentUserId?: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSave: (roleNames: string[]) => Promise<void>;
}

const ADMIN_MANAGEMENT_SCOPES = new Set(['admin.users', 'admin.full']);

export function UserRolesDialog({
  user,
  availableRoles,
  currentUserId,
  open,
  onOpenChange,
  onSave,
}: UserRolesDialogProps) {
  const [selectedRoles, setSelectedRoles] = useState<string[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    setSelectedRoles(user?.roles ?? []);
  }, [user]);

  const selectedRoleSet = useMemo(() => new Set(selectedRoles), [selectedRoles]);

  const hasChanges = useMemo(() => {
    if (!user) {
      return false;
    }

    const left = [...selectedRoles].sort().join('|');
    const right = [...user.roles].sort().join('|');
    return left !== right;
  }, [selectedRoles, user]);

  const roleToScopes = useMemo(() => {
    const map = new Map<string, string[]>();
    for (const role of availableRoles) {
      map.set(role.name, role.assignedScopes);
    }
    return map;
  }, [availableRoles]);

  const selectedScopes = useMemo(() => {
    const scopes = new Set<string>();
    for (const roleName of selectedRoles) {
      for (const scope of roleToScopes.get(roleName) ?? []) {
        scopes.add(scope);
      }
    }
    return scopes;
  }, [roleToScopes, selectedRoles]);

  const wouldLoseSelfAdminManagement = useMemo(() => {
    if (!user || !currentUserId || user.id !== currentUserId) {
      return false;
    }

    for (const requiredScope of ADMIN_MANAGEMENT_SCOPES) {
      if (selectedScopes.has(requiredScope)) {
        return false;
      }
    }

    return true;
  }, [currentUserId, selectedScopes, user]);

  const toggleRole = (roleName: string) => {
    setSelectedRoles((current) =>
      current.includes(roleName)
        ? current.filter((name) => name !== roleName)
        : [...current, roleName].sort()
    );
  };

  const canSubmit = hasChanges && !isSubmitting && !wouldLoseSelfAdminManagement;

  const handleSave = async () => {
    if (!canSubmit) {
      return;
    }

    setIsSubmitting(true);
    try {
      await onSave(selectedRoles);
      onOpenChange(false);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Edit Roles</DialogTitle>
          <DialogDescription>
            Assign active roles for this user. Inactive roles are disabled.
          </DialogDescription>
        </DialogHeader>

        <div className="max-h-64 space-y-2 overflow-y-auto rounded-md border p-3">
          {availableRoles.map((role) => (
            <label key={role.id} className="flex items-start gap-2 text-sm">
              <input
                type="checkbox"
                checked={selectedRoleSet.has(role.name)}
                onChange={() => toggleRole(role.name)}
                disabled={!role.isActive || isSubmitting}
              />
              <span>
                {role.name}
                {!role.isActive ? <span className="text-destructive"> (inactive)</span> : null}
                {role.description ? <span className="text-muted-foreground"> - {role.description}</span> : null}
              </span>
            </label>
          ))}
        </div>

        {wouldLoseSelfAdminManagement ? (
          <p className="text-xs text-destructive">
            You cannot remove your own admin management scopes.
          </p>
        ) : null}

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="button" onClick={handleSave} disabled={!canSubmit} data-testid="edit-user-roles-submit">
            {isSubmitting ? 'Saving...' : 'Save Roles'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
