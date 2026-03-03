import type { ReactNode } from 'react';

import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

type SectionHeaderProps = {
  title: string;
  description?: string;
  action?: ReactNode;
};

export function SectionHeader({
  title,
  description,
  action,
}: SectionHeaderProps) {
  return (
    <Stack
      direction="row"
      spacing={2}
      alignItems="flex-start"
      justifyContent="space-between"
    >
      <Stack spacing={0.5}>
        <Typography variant="h6">{title}</Typography>
        {description ? (
          <Typography variant="body2" color="text.secondary">
            {description}
          </Typography>
        ) : null}
      </Stack>

      {action}
    </Stack>
  );
}
