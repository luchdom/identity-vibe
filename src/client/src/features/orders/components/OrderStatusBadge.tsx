import { Badge } from '@/components/ui/badge';
import { OrderStatus, OrderStatusLabels, OrderStatusColors } from '@/types/orders';
import { cn } from '@/lib/utils';

interface OrderStatusBadgeProps {
  status: OrderStatus;
  className?: string;
  variant?: 'default' | 'secondary' | 'destructive' | 'outline';
}

export function OrderStatusBadge({ status, className, variant }: OrderStatusBadgeProps) {
  const statusConfig = OrderStatusColors[status];
  const statusLabel = OrderStatusLabels[status];

  if (variant) {
    // Use standard badge variants if specified
    return (
      <Badge variant={variant} className={className}>
        {statusLabel}
      </Badge>
    );
  }

  // Use custom status-specific styling
  return (
    <Badge
      className={cn(
        'border font-medium',
        statusConfig.bg,
        statusConfig.text,
        statusConfig.border,
        className
      )}
      variant="outline"
    >
      {statusLabel}
    </Badge>
  );
}