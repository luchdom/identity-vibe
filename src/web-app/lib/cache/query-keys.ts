import type { OrderStatus } from '@/types/orders';

const stringify = (value: string | number | boolean | undefined) =>
  value === undefined ? 'any' : String(value);

export const USERS_LIST_CACHE_PREFIX = 'users:list';
export const USERS_AVAILABLE_ROLES_CACHE_KEY = 'users:available-roles';

export const ROLES_LIST_CACHE_PREFIX = 'roles:list';
export const ROLES_AVAILABLE_SCOPES_CACHE_KEY = 'roles:available-scopes';
export const ROLE_DETAIL_CACHE_PREFIX = 'roles:detail';

export const ORDERS_LIST_CACHE_PREFIX = 'orders:list';
export const ORDERS_STATUS_COUNTS_CACHE_PREFIX = 'orders:status-counts';
export const ORDER_DETAIL_CACHE_PREFIX = 'orders:detail';

type UsersListKeyParams = {
  search?: string;
  isActive?: boolean;
  role?: string;
  pageNumber: number;
  pageSize: number;
};

export function usersListCacheKey(params: UsersListKeyParams): string {
  return `${USERS_LIST_CACHE_PREFIX}:search=${stringify(params.search)}:status=${stringify(params.isActive)}:role=${stringify(params.role)}:page=${params.pageNumber}:size=${params.pageSize}`;
}

type OrdersListKeyParams = {
  scope: 'all' | 'mine';
  status?: OrderStatus;
  page: number;
  pageSize: number;
};

export function ordersListCacheKey(params: OrdersListKeyParams): string {
  return `${ORDERS_LIST_CACHE_PREFIX}:scope=${params.scope}:status=${stringify(params.status)}:page=${params.page}:size=${params.pageSize}`;
}

export function ordersStatusCountsCacheKey(scope: 'all' | 'mine'): string {
  return `${ORDERS_STATUS_COUNTS_CACHE_PREFIX}:scope=${scope}`;
}

export function orderDetailCacheKey(orderId: number): string {
  return `${ORDER_DETAIL_CACHE_PREFIX}:${orderId}`;
}

export function rolesListCacheKey(search = ''): string {
  return `${ROLES_LIST_CACHE_PREFIX}:search=${search.trim().toLowerCase() || 'any'}`;
}

export function roleDetailCacheKey(roleId: string): string {
  return `${ROLE_DETAIL_CACHE_PREFIX}:${roleId}`;
}

