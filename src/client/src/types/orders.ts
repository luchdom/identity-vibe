export enum OrderStatus {
  Draft = 0,
  Confirmed = 1,
  Processing = 2,
  Shipped = 3,
  Delivered = 4,
  Cancelled = 5,
}

export interface CustomerResponse {
  id: string;
  name: string;
  email: string;
  phone?: string;
  address?: string;
}

export interface OrderItemResponse {
  id: number;
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  imageUrl?: string;
}

export interface OrderStatusHistoryResponse {
  id: number;
  status: OrderStatus;
  statusDisplay: string;
  timestamp: string;
  note?: string;
  changedBy?: string;
}

export interface OrderSummaryResponse {
  id: number;
  orderNumber: string;
  status: OrderStatus;
  statusDisplay: string;
  customerName: string;
  customerEmail: string;
  totalAmount: number;
  currency: string;
  itemCount: number;
  createdAt: string;
  updatedAt: string;
  shippedAt?: string;
  canBeCancelled: boolean;
  canBeModified: boolean;
}

export interface OrderResponse {
  id: number;
  orderNumber: string;
  status: OrderStatus;
  statusDisplay: string;
  customer: CustomerResponse;
  items: OrderItemResponse[];
  subtotalAmount: number;
  taxAmount: number;
  shippingAmount: number;
  discountAmount: number;
  totalAmount: number;
  currency: string;
  trackingNumber?: string;
  createdAt: string;
  updatedAt: string;
  shippedAt?: string;
  deliveredAt?: string;
  cancelledAt?: string;
  itemCount: number;
  canBeCancelled: boolean;
  canBeModified: boolean;
  statusHistory: OrderStatusHistoryResponse[];
}

export interface OrderListResponse {
  orders: OrderSummaryResponse[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface OrderQuery {
  page?: number;
  pageSize?: number;
  status?: OrderStatus;
  search?: string;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

export interface UpdateOrderStatusRequest {
  status: OrderStatus;
  note?: string;
}

export interface CancelOrderRequest {
  reason?: string;
}

export interface FilterState {
  search: string;
  status?: OrderStatus;
  sortBy: string;
  sortOrder: 'asc' | 'desc';
}

export const OrderStatusLabels: Record<OrderStatus, string> = {
  [OrderStatus.Draft]: 'Draft',
  [OrderStatus.Confirmed]: 'Confirmed',
  [OrderStatus.Processing]: 'Processing',
  [OrderStatus.Shipped]: 'Shipped',
  [OrderStatus.Delivered]: 'Delivered',
  [OrderStatus.Cancelled]: 'Cancelled',
};

export const OrderStatusColors: Record<OrderStatus, { bg: string; text: string; border: string }> = {
  [OrderStatus.Draft]: { 
    bg: 'bg-gray-100 dark:bg-gray-800', 
    text: 'text-gray-700 dark:text-gray-300', 
    border: 'border-gray-200 dark:border-gray-700' 
  },
  [OrderStatus.Confirmed]: { 
    bg: 'bg-blue-100 dark:bg-blue-900', 
    text: 'text-blue-700 dark:text-blue-300', 
    border: 'border-blue-200 dark:border-blue-700' 
  },
  [OrderStatus.Processing]: { 
    bg: 'bg-yellow-100 dark:bg-yellow-900', 
    text: 'text-yellow-700 dark:text-yellow-300', 
    border: 'border-yellow-200 dark:border-yellow-700' 
  },
  [OrderStatus.Shipped]: { 
    bg: 'bg-purple-100 dark:bg-purple-900', 
    text: 'text-purple-700 dark:text-purple-300', 
    border: 'border-purple-200 dark:border-purple-700' 
  },
  [OrderStatus.Delivered]: { 
    bg: 'bg-green-100 dark:bg-green-900', 
    text: 'text-green-700 dark:text-green-300', 
    border: 'border-green-200 dark:border-green-700' 
  },
  [OrderStatus.Cancelled]: { 
    bg: 'bg-red-100 dark:bg-red-900', 
    text: 'text-red-700 dark:text-red-300', 
    border: 'border-red-200 dark:border-red-700' 
  },
};