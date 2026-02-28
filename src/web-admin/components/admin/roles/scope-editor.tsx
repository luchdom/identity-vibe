'use client';

import { useMemo } from 'react';
import type { AvailableScope } from '@/lib/admin-roles-api';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { Badge } from '@/components/ui/badge';
import { ChevronDown, ChevronRight } from 'lucide-react';

interface RoleScopeEditorProps {
  availableScopes: AvailableScope[];
  selectedScopes: string[];
  onToggleScope: (scopeName: string) => void;
  onReplaceGroup: (group: string, scopeNames: string[]) => void;
  disabled?: boolean;
}

export function RoleScopeEditor({ availableScopes, selectedScopes, onToggleScope, onReplaceGroup, disabled = false }: RoleScopeEditorProps) {
  const groups = useMemo(() => {
    const grouped = new Map<string, AvailableScope[]>();
    for (const scope of availableScopes) {
      const list = grouped.get(scope.group) ?? [];
      list.push(scope);
      grouped.set(scope.group, list);
    }

    return Array.from(grouped.entries())
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([group, scopes]) => ({
        group,
        scopes: scopes.sort((a, b) => a.name.localeCompare(b.name)),
      }));
  }, [availableScopes]);

  const selectedSet = useMemo(() => new Set(selectedScopes), [selectedScopes]);

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base">Permission Scopes</CardTitle>
        <CardDescription>
          Assign permission scopes to the selected role. API access is authorized by these scopes.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-3" data-testid="role-scope-editor">
        {groups.map(({ group, scopes }) => {
          const selectedInGroup = scopes.filter((scope) => selectedSet.has(scope.name)).length;
          const allNames = scopes.map((scope) => scope.name);
          return (
            <Collapsible key={group} defaultOpen>
              <div className="rounded-lg border">
                <div className="flex items-center justify-between gap-2 px-3 py-2">
                  <CollapsibleTrigger asChild>
                    <button type="button" className="flex items-center gap-2 text-left font-medium">
                      <ChevronRight className="hidden h-4 w-4 data-[state=open]:hidden" />
                      <ChevronDown className="h-4 w-4" />
                      <span className="capitalize">{group}</span>
                      <Badge variant="outline">{selectedInGroup}/{scopes.length}</Badge>
                    </button>
                  </CollapsibleTrigger>
                  <div className="flex gap-2">
                    <Button
                      type="button"
                      size="sm"
                      variant="ghost"
                      disabled={disabled}
                      onClick={() => onReplaceGroup(group, Array.from(new Set([...selectedScopes.filter(s => !allNames.includes(s)), ...allNames])))}
                    >
                      All
                    </Button>
                    <Button
                      type="button"
                      size="sm"
                      variant="ghost"
                      disabled={disabled}
                      onClick={() => onReplaceGroup(group, selectedScopes.filter(s => !allNames.includes(s)))}
                    >
                      Clear
                    </Button>
                  </div>
                </div>
                <CollapsibleContent>
                  <div className="space-y-2 border-t px-3 py-3">
                    {scopes.map((scope) => (
                      <label key={scope.name} className="flex items-start gap-3 rounded-md border p-2 hover:bg-muted/40">
                        <input
                          type="checkbox"
                          className="mt-1 h-4 w-4"
                          checked={selectedSet.has(scope.name)}
                          disabled={disabled}
                          onChange={() => onToggleScope(scope.name)}
                        />
                        <div className="min-w-0">
                          <div className="text-sm font-medium">{scope.displayName || scope.name}</div>
                          <div className="text-xs text-muted-foreground">{scope.name}</div>
                          {scope.description ? (
                            <div className="mt-1 text-xs text-muted-foreground">{scope.description}</div>
                          ) : null}
                        </div>
                      </label>
                    ))}
                  </div>
                </CollapsibleContent>
              </div>
            </Collapsible>
          );
        })}
      </CardContent>
    </Card>
  );
}
