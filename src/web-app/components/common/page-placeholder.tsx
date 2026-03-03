import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Chip from '@mui/material/Chip';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

type PagePlaceholderProps = {
  eyebrow?: string;
  title: string;
  description: string;
  note?: string;
};

export function PagePlaceholder({
  eyebrow = 'Coming soon',
  title,
  description,
  note,
}: PagePlaceholderProps) {
  return (
    <Card>
      <CardContent sx={{ p: { xs: 3, md: 4 } }}>
        <Stack spacing={2.5}>
          <Chip
            label={eyebrow}
            color="primary"
            variant="outlined"
            sx={{ alignSelf: 'flex-start' }}
          />

          <Stack spacing={1}>
            <Typography variant="h4">{title}</Typography>
            <Typography color="text.secondary" sx={{ maxWidth: 720 }}>
              {description}
            </Typography>
          </Stack>

          {note ? (
            <Typography variant="body2" color="text.secondary">
              {note}
            </Typography>
          ) : null}
        </Stack>
      </CardContent>
    </Card>
  );
}
