import { AppLabel } from '@/components/common/app-label';
import { OrderStatus } from '@/types/orders';

type OrderStatusLabelProps = {
  status: OrderStatus;
  statusDisplay?: string;
};

function getStatusColor(status: OrderStatus): 'default' | 'success' | 'warning' | 'error' | 'info' | 'primary' {
  switch (status) {
    case OrderStatus.Delivered:
      return 'success';
    case OrderStatus.Shipped:
      return 'info';
    case OrderStatus.Processing:
      return 'primary';
    case OrderStatus.Confirmed:
      return 'default';
    case OrderStatus.Cancelled:
      return 'error';
    case OrderStatus.Draft:
    default:
      return 'warning';
  }
}

function getStatusText(status: OrderStatus): string {
  switch (status) {
    case OrderStatus.Draft:
      return 'Draft';
    case OrderStatus.Confirmed:
      return 'Confirmed';
    case OrderStatus.Processing:
      return 'Processing';
    case OrderStatus.Shipped:
      return 'Shipped';
    case OrderStatus.Delivered:
      return 'Delivered';
    case OrderStatus.Cancelled:
      return 'Cancelled';
    default:
      return String(status);
  }
}

export function OrderStatusLabel({ status, statusDisplay }: OrderStatusLabelProps) {
  return (
    <AppLabel color={getStatusColor(status)}>
      {statusDisplay || getStatusText(status)}
    </AppLabel>
  );
}

