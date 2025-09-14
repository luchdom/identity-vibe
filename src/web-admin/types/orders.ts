export enum OrderStatus {
  Draft = 0,
  Confirmed = 1,
  Processing = 2,
  Shipped = 3,
  Delivered = 4,
  Cancelled = 5,
}

export interface OrderSummary {
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

export interface Customer {
  name: string;
  email: string;
  phone?: string;
  address?: string;
  city?: string;
  state?: string;
  postalCode?: string;
  country?: string;
}

export interface OrderItem {
  id: number;
  productName: string;
  sku?: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  description?: string;
}

export interface Shipping {
  method: string;
  carrier?: string;
  trackingNumber?: string;
  estimatedDelivery?: string;
  address: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

export interface OrderStatusHistory {
  id: number;
  status: OrderStatus;
  statusDisplay: string;
  notes?: string;
  createdAt: string;
  createdBy: string;
}

export interface OrderDetails {
  id: number;
  orderNumber: string;
  status: OrderStatus;
  statusDisplay: string;
  customer: Customer;
  items: OrderItem[];
  subtotalAmount: number;
  taxAmount: number;
  shippingAmount: number;
  discountAmount: number;
  totalAmount: number;
  currency: string;
  trackingNumber?: string;
  paymentMethod?: string;
  paymentReference?: string;
  shipping?: Shipping;
  specialInstructions?: string;
  createdAt: string;
  updatedAt: string;
  shippedAt?: string;
  deliveredAt?: string;
  cancelledAt?: string;
  itemCount: number;
  canBeCancelled: boolean;
  canBeModified: boolean;
  statusHistory: OrderStatusHistory[];
}

export interface OrderListResponse {
  orders: OrderSummary[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface OrdersQueryParams {
  page?: number;
  pageSize?: number;
  status?: OrderStatus;
}

export interface CreateOrderItem {
  productName: string;
  sku?: string;
  quantity: number;
  unitPrice: number;
  description?: string;
}

export interface CreateOrderRequest {
  customer: Omit<Customer, 'id'>;
  items: CreateOrderItem[];
  shipping?: Omit<Shipping, 'trackingNumber' | 'estimatedDelivery'>;
  paymentMethod?: string;
  specialInstructions?: string;
}

export interface UpdateOrderRequest {
  customer?: Partial<Customer>;
  items?: CreateOrderItem[];
  shipping?: Partial<Shipping>;
  specialInstructions?: string;
}

export interface UpdateOrderStatusRequest {
  status: OrderStatus;
  notes?: string;
}

export interface AddTrackingNumberRequest {
  trackingNumber: string;
  carrier?: string;
  estimatedDelivery?: string;
}