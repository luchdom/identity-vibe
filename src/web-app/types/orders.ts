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

export interface CustomerInfo {
  firstName: string;
  lastName: string;
  fullName: string;
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
  productSku?: string;
  productName: string;
  productDescription?: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface ShippingInfo {
  address?: string;
  city?: string;
  state?: string;
  postalCode?: string;
  country?: string;
  shippingMethod?: string;
  shippingCost?: number;
}

export interface OrderStatusHistory {
  id: number;
  toStatus: OrderStatus;
  statusChange?: string;
  notes?: string;
  reason?: string;
  changedByUserId: string;
  changedByUserName?: string;
  createdAt: string;
}

export interface OrderDetails {
  id: number;
  orderNumber: string;
  status: OrderStatus;
  statusDisplay: string;
  customer: CustomerInfo;
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
  shipping?: ShippingInfo;
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

export interface CreateOrderItemRequest {
  productSku: string;
  productName: string;
  productDescription?: string;
  quantity: number;
  unitPrice: number;
}

export interface CreateOrderRequest {
  customer: {
    firstName: string;
    lastName: string;
    email: string;
    phone?: string;
    address?: string;
    city?: string;
    state?: string;
    postalCode?: string;
    country?: string;
  };
  items: CreateOrderItemRequest[];
  shipping?: {
    address?: string;
    city?: string;
    state?: string;
    postalCode?: string;
    country?: string;
    shippingCost?: number;
  };
  specialInstructions?: string;
}
