'use client';

import { useMemo, useState } from 'react';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import type { AdminRoleOption, CreateUserPayload } from '@/lib/admin-users-api';

interface CreateUserDialogProps {
  availableRoles: AdminRoleOption[];
  onCreate: (payload: CreateUserPayload) => Promise<void>;
}

export function CreateUserDialog({ availableRoles, onCreate }: CreateUserDialogProps) {
  const [open, setOpen] = useState(false);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [isActive, setIsActive] = useState(true);
  const [selectedRoles, setSelectedRoles] = useState<string[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const selectableRoles = useMemo(
    () => availableRoles.filter((role) => role.isActive),
    [availableRoles]
  );

  const canSubmit = useMemo(() => {
    return email.trim().length > 0 && password.length > 0 && selectedRoles.length > 0 && !isSubmitting;
  }, [email, password, selectedRoles, isSubmitting]);

  const toggleRole = (roleName: string) => {
    setSelectedRoles((current) =>
      current.includes(roleName)
        ? current.filter((name) => name !== roleName)
        : [...current, roleName].sort()
    );
  };

  const resetForm = () => {
    setEmail('');
    setPassword('');
    setPhoneNumber('');
    setIsActive(true);
    setSelectedRoles([]);
  };

  const handleCreate = async () => {
    if (!canSubmit) {
      return;
    }

    setIsSubmitting(true);
    try {
      await onCreate({
        email: email.trim(),
        password,
        phoneNumber: phoneNumber.trim() || undefined,
        isActive,
        roleNames: selectedRoles,
      });
      resetForm();
      setOpen(false);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button data-testid="new-user-button">New User</Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Create User</DialogTitle>
          <DialogDescription>
            Create an account and assign at least one active role.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-1">
            <label className="text-sm font-medium" htmlFor="create-user-email">Email</label>
            <input
              id="create-user-email"
              data-testid="create-user-email"
              className="h-9 w-full rounded-md border px-3 text-sm"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="user@example.com"
              disabled={isSubmitting}
            />
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium" htmlFor="create-user-password">Password</label>
            <input
              id="create-user-password"
              data-testid="create-user-password"
              type="password"
              className="h-9 w-full rounded-md border px-3 text-sm"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              disabled={isSubmitting}
            />
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium" htmlFor="create-user-phone">Phone Number</label>
            <input
              id="create-user-phone"
              data-testid="create-user-phone"
              className="h-9 w-full rounded-md border px-3 text-sm"
              value={phoneNumber}
              onChange={(e) => setPhoneNumber(e.target.value)}
              placeholder="+1 555 0100"
              disabled={isSubmitting}
            />
          </div>

          <label className="flex items-center gap-2 text-sm">
            <input
              data-testid="create-user-active"
              type="checkbox"
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
              disabled={isSubmitting}
            />
            <span>Active</span>
          </label>

          <div className="space-y-2">
            <p className="text-sm font-medium">Roles</p>
            <div className="max-h-44 space-y-2 overflow-y-auto rounded-md border p-2">
              {selectableRoles.map((role) => (
                <label key={role.id} className="flex items-start gap-2 text-sm">
                  <input
                    type="checkbox"
                    checked={selectedRoles.includes(role.name)}
                    onChange={() => toggleRole(role.name)}
                    disabled={isSubmitting}
                  />
                  <span>
                    {role.name}
                    {role.description ? <span className="text-muted-foreground"> - {role.description}</span> : null}
                  </span>
                </label>
              ))}
              {selectableRoles.length === 0 ? (
                <p className="text-xs text-muted-foreground">No active roles available.</p>
              ) : null}
            </div>
          </div>
        </div>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => setOpen(false)} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="button" onClick={handleCreate} disabled={!canSubmit} data-testid="create-user-submit">
            {isSubmitting ? 'Creating...' : 'Create'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
