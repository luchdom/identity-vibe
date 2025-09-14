import { Badge } from '@/components/ui/badge';
import { OrderStatus } from '@/types/orders';
import { cn } from '@/lib/utils';

interface OrderStatusBadgeProps {
  status: OrderStatus;
  className?: string;
}

const statusConfig = {
  [OrderStatus.Draft]: {
    label: 'Draft',
    variant: 'secondary' as const,
    className: 'bg-gray-100 text-gray-800 hover:bg-gray-200',
  },
  [OrderStatus.Confirmed]: {
    label: 'Confirmed',
    variant: 'default' as const,
    className: 'bg-blue-100 text-blue-800 hover:bg-blue-200',
  },
  [OrderStatus.Processing]: {
    label: 'Processing',
    variant: 'default' as const,
    className: 'bg-yellow-100 text-yellow-800 hover:bg-yellow-200',
  },
  [OrderStatus.Shipped]: {
    label: 'Shipped',
    variant: 'default' as const,
    className: 'bg-purple-100 text-purple-800 hover:bg-purple-200',
  },
  [OrderStatus.Delivered]: {
    label: 'Delivered',
    variant: 'default' as const,
    className: 'bg-green-100 text-green-800 hover:bg-green-200',
  },
  [OrderStatus.Cancelled]: {
    label: 'Cancelled',
    variant: 'destructive' as const,
    className: 'bg-red-100 text-red-800 hover:bg-red-200',
  },
};

export function OrderStatusBadge({ status, className }: OrderStatusBadgeProps) {
  const config = statusConfig[status] || statusConfig[OrderStatus.Draft];
  
  return (
    <Badge 
      variant={config.variant}
      className={cn(config.className, className)}
    >
      {config.label}
    </Badge>
  );
}