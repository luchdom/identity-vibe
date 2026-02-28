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
import { adminRolesApi } from '@/lib/admin-roles-api';
import { toast } from 'sonner';

interface CreateRoleDialogProps {
  onCreated: (roleId: string) => Promise<void> | void;
}

function getErrorMessage(error: unknown): string {
  if (typeof error === 'object' && error !== null) {
    const maybeAxios = error as { response?: { data?: { detail?: string } }; message?: string };
    return maybeAxios.response?.data?.detail || maybeAxios.message || 'Failed to create role';
  }

  return 'Failed to create role';
}

export function CreateRoleDialog({ onCreated }: CreateRoleDialogProps) {
  const [open, setOpen] = useState(false);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [isSaving, setIsSaving] = useState(false);

  const nameError = useMemo(() => {
    const trimmed = name.trim();
    if (!trimmed) return 'Role name is required.';
    if (!/^[A-Za-z0-9._-]{2,64}$/.test(trimmed)) {
      return 'Use 2-64 chars: letters, numbers, dot, dash, underscore.';
    }
    return null;
  }, [name]);

  const handleSubmit = async () => {
    if (nameError) {
      toast.error(nameError);
      return;
    }

    setIsSaving(true);
    try {
      const created = await adminRolesApi.createRole({
        name: name.trim(),
        description: description.trim() || undefined,
        isActive: true,
      });
      toast.success(`Role '${created.name}' created`);
      setOpen(false);
      setName('');
      setDescription('');
      await onCreated(created.id);
    } catch (error: unknown) {
      toast.error(getErrorMessage(error));
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button data-testid="new-role-button">New Role</Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Create Role</DialogTitle>
          <DialogDescription>
            Add a new role and then assign permission scopes.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-1">
            <label className="text-sm font-medium" htmlFor="role-name">Role name</label>
            <input
              id="role-name"
              className="h-9 w-full rounded-md border px-3 text-sm"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="e.g. Auditor"
              disabled={isSaving}
            />
            {name && nameError ? <p className="text-xs text-red-600">{nameError}</p> : null}
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium" htmlFor="role-description">Description</label>
            <textarea
              id="role-description"
              className="min-h-24 w-full rounded-md border px-3 py-2 text-sm"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Describe what this role is used for"
              disabled={isSaving}
            />
          </div>
        </div>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => setOpen(false)} disabled={isSaving}>Cancel</Button>
          <Button type="button" onClick={handleSubmit} disabled={isSaving || !!nameError}>Create</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
