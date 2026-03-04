'use client';

import { useMemo } from 'react';

import Accordion from '@mui/material/Accordion';
import AccordionDetails from '@mui/material/AccordionDetails';
import AccordionSummary from '@mui/material/AccordionSummary';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Checkbox from '@mui/material/Checkbox';
import Divider from '@mui/material/Divider';
import FormControlLabel from '@mui/material/FormControlLabel';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import { Icon } from '@iconify/react';

import { AppLabel } from '@/components/common/app-label';
import { SurfacePanel } from '@/components/common/surface-panel';
import type { AvailableScope } from '@/lib/admin-roles-api';

type RoleScopeEditorProps = {
  availableScopes: AvailableScope[];
  selectedScopes: string[];
  onToggleScope: (scopeName: string) => void;
  onReplaceGroup: (group: string, scopeNames: string[]) => void;
  disabled?: boolean;
};

export function RoleScopeEditor({
  availableScopes,
  selectedScopes,
  onToggleScope,
  onReplaceGroup,
  disabled = false,
}: RoleScopeEditorProps) {
  const groups = useMemo(() => {
    const grouped = new Map<string, AvailableScope[]>();

    for (const scope of availableScopes) {
      const list = grouped.get(scope.group) ?? [];
      list.push(scope);
      grouped.set(scope.group, list);
    }

    return Array.from(grouped.entries())
      .sort(([left], [right]) => left.localeCompare(right))
      .map(([group, scopes]) => ({
        group,
        scopes: scopes.sort((left, right) => left.name.localeCompare(right.name)),
      }));
  }, [availableScopes]);

  const selectedSet = useMemo(() => new Set(selectedScopes), [selectedScopes]);

  return (
    <SurfacePanel>
      <Stack spacing={1} sx={{ p: 2 }}>
        <Typography variant="subtitle1" fontWeight={700}>
          Permission scopes
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Assign scopes grouped by domain. API authorization policies evaluate these permissions.
        </Typography>
      </Stack>

      <Divider />

      <Stack spacing={1.25} sx={{ p: 1.5 }} data-testid="role-scope-editor">
        {groups.map(({ group, scopes }) => {
          const groupScopeNames = scopes.map((scope) => scope.name);
          const selectedInGroup = scopes.filter((scope) => selectedSet.has(scope.name)).length;

          return (
            <Accordion
              key={group}
              disableGutters
              defaultExpanded
              elevation={0}
              sx={{
                border: '1px solid',
                borderColor: 'divider',
                borderRadius: 1,
                '&:before': {
                  display: 'none',
                },
              }}
            >
              <AccordionSummary
                expandIcon={<Icon icon="eva:arrow-ios-downward-fill" width={16} />}
                sx={{
                  minHeight: 46,
                  '& .MuiAccordionSummary-content': {
                    m: 0,
                  },
                }}
              >
                <Stack direction="row" spacing={1} alignItems="center">
                  <Typography variant="subtitle2" sx={{ textTransform: 'capitalize' }}>
                    {group}
                  </Typography>
                  <AppLabel>
                    {selectedInGroup}/{scopes.length}
                  </AppLabel>
                </Stack>
              </AccordionSummary>
              <AccordionDetails sx={{ pt: 0, px: 1.5, pb: 1.5 }}>
                <Stack spacing={1.25}>
                  <Stack direction="row" spacing={1}>
                    <Button
                      size="small"
                      variant="outlined"
                      disabled={disabled}
                      onClick={() => {
                        onReplaceGroup(
                          group,
                          Array.from(new Set([...selectedScopes.filter((value) => !groupScopeNames.includes(value)), ...groupScopeNames]))
                        );
                      }}
                    >
                      All
                    </Button>
                    <Button
                      size="small"
                      variant="outlined"
                      disabled={disabled}
                      onClick={() => {
                        onReplaceGroup(
                          group,
                          selectedScopes.filter((value) => !groupScopeNames.includes(value))
                        );
                      }}
                    >
                      Clear
                    </Button>
                  </Stack>

                  <Stack spacing={0.5}>
                    {scopes.map((scope) => (
                      <Box
                        key={scope.name}
                        sx={{
                          border: '1px solid',
                          borderColor: 'divider',
                          borderRadius: 1,
                          px: 1.25,
                          py: 0.75,
                        }}
                      >
                        <FormControlLabel
                          control={(
                            <Checkbox
                              size="small"
                              checked={selectedSet.has(scope.name)}
                              disabled={disabled}
                              onChange={() => onToggleScope(scope.name)}
                            />
                          )}
                          label={(
                            <Stack spacing={0.25}>
                              <Typography variant="body2" fontWeight={600}>
                                {scope.displayName || scope.name}
                              </Typography>
                              <Typography variant="caption" color="text.secondary">
                                {scope.name}
                              </Typography>
                              {scope.description ? (
                                <Typography variant="caption" color="text.secondary">
                                  {scope.description}
                                </Typography>
                              ) : null}
                            </Stack>
                          )}
                          sx={{ alignItems: 'flex-start', m: 0 }}
                        />
                      </Box>
                    ))}
                  </Stack>
                </Stack>
              </AccordionDetails>
            </Accordion>
          );
        })}
      </Stack>
    </SurfacePanel>
  );
}

