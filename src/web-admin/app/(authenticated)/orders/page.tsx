'use client';

import { useSearchParams, useRouter, usePathname } from 'next/navigation';
import { useMemo, useEffect } from 'react';
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb"
import { Separator } from "@/components/ui/separator"
import { SidebarTrigger } from "@/components/ui/sidebar"
import { OrdersTable } from "@/components/orders-table"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"
import { useOrdersWithStatus } from "@/hooks/use-orders"
import { ordersApi } from "@/lib/orders-api"
import { OrderStatus } from "@/types/orders"
import { RefreshCw, Plus } from "lucide-react"
import { useAuth } from "@/contexts/auth-context"
import { toast } from "sonner"

const STATUS_FILTERS: Array<{ label: string; value?: OrderStatus }> = [
  { label: 'All', value: undefined },
  { label: 'Confirmed', value: OrderStatus.Confirmed },
  { label: 'Processing', value: OrderStatus.Processing },
  { label: 'Shipped', value: OrderStatus.Shipped },
  { label: 'Delivered', value: OrderStatus.Delivered },
  { label: 'Cancelled', value: OrderStatus.Cancelled },
];

export default function OrdersPage() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();
  const { scopes } = useAuth();
  
  // Get status from URL params
  const statusParam = searchParams.get('status');
  const pageParam = searchParams.get('page');
  
  const status = statusParam ? parseInt(statusParam) as OrderStatus : undefined;
  const page = pageParam ? parseInt(pageParam) : 1;
  const pageSize = 20;

  const { data, loading, error, refetch } = useOrdersWithStatus(status, page, pageSize);
  const isAdmin = useMemo(() => ordersApi.isAdmin(), []);
  const canCreateOrder = useMemo(
    () => scopes.includes('orders.write') || scopes.includes('orders.manage'),
    [scopes]
  );

  useEffect(() => {
    if (searchParams.get('created') !== '1') {
      return;
    }

    toast.success('Order created successfully');

    const nextParams = new URLSearchParams(searchParams.toString());
    nextParams.delete('created');
    const nextQuery = nextParams.toString();
    router.replace(nextQuery ? `${pathname}?${nextQuery}` : pathname);
  }, [searchParams, router, pathname]);

  const updateQueryParams = (mutate: (params: URLSearchParams) => void) => {
    const nextParams = new URLSearchParams(searchParams.toString());
    mutate(nextParams);
    const nextQuery = nextParams.toString();
    router.push(nextQuery ? `${pathname}?${nextQuery}` : pathname);
  };

  const handleStatusFilterChange = (nextStatus?: OrderStatus) => {
    updateQueryParams((params) => {
      if (nextStatus === undefined) {
        params.delete('status');
      } else {
        params.set('status', nextStatus.toString());
      }

      params.delete('page');
    });
  };

  // Loading state
  if (loading) {
    return (
      <>
        <header className="flex h-16 shrink-0 items-center gap-2 border-b px-4">
          <SidebarTrigger className="-ml-1" />
          <Separator
            orientation="vertical"
            className="mr-2 data-[orientation=vertical]:h-4"
          />
          <Breadcrumb>
            <BreadcrumbList>
              <BreadcrumbItem className="hidden md:block">
                <BreadcrumbLink href="#">
                  Orders Management
                </BreadcrumbLink>
              </BreadcrumbItem>
              <BreadcrumbSeparator className="hidden md:block" />
              <BreadcrumbItem>
                <BreadcrumbPage>
                  {status !== undefined ? getStatusDisplayName(status) : 'All Orders'}
                </BreadcrumbPage>
              </BreadcrumbItem>
            </BreadcrumbList>
          </Breadcrumb>
        </header>
        <div className="flex flex-1 flex-col gap-4 p-4">
          <div className="flex items-center justify-between">
            <Skeleton className="h-8 w-32" />
            <Skeleton className="h-9 w-24" />
          </div>
          <div className="space-y-3">
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-10 w-full" />
          </div>
        </div>
      </>
    );
  }

  // Error state
  if (error) {
    return (
      <>
        <header className="flex h-16 shrink-0 items-center gap-2 border-b px-4">
          <SidebarTrigger className="-ml-1" />
          <Separator
            orientation="vertical"
            className="mr-2 data-[orientation=vertical]:h-4"
          />
          <Breadcrumb>
            <BreadcrumbList>
              <BreadcrumbItem className="hidden md:block">
                <BreadcrumbLink href="#">
                  Orders Management
                </BreadcrumbLink>
              </BreadcrumbItem>
              <BreadcrumbSeparator className="hidden md:block" />
              <BreadcrumbItem>
                <BreadcrumbPage>Error</BreadcrumbPage>
              </BreadcrumbItem>
            </BreadcrumbList>
          </Breadcrumb>
        </header>
        <div className="flex flex-1 flex-col gap-4 p-4">
          <div className="bg-destructive/15 border border-destructive/20 rounded-lg p-4">
            <p className="text-destructive font-medium">Failed to load orders</p>
            <p className="text-destructive/80 text-sm mt-1">{error}</p>
            <Button 
              variant="outline" 
              size="sm" 
              className="mt-3"
              onClick={() => refetch()}
            >
              <RefreshCw className="w-4 h-4 mr-2" />
              Try Again
            </Button>
          </div>
        </div>
      </>
    );
  }

  const handleViewOrder = (orderId: number) => {
    router.push(`/orders/${orderId}`);
  };

  const handleCancelOrder = async (orderId: number) => {
    try {
      await ordersApi.cancelOrder(orderId, 'Cancelled by admin');
      await refetch();
    } catch (error) {
      console.error('Failed to cancel order:', error);
    }
  };

  const handleAddTracking = (orderId: number) => {
    // This would open a modal - for now just navigate to order detail
    router.push(`/orders/${orderId}`);
  };

  const handleNewOrderClick = () => {
    if (!canCreateOrder) {
      return;
    }

    router.push('/orders/new');
  };

  return (
    <>
      <header className="flex h-16 shrink-0 items-center gap-2 border-b px-4">
        <SidebarTrigger className="-ml-1" />
        <Separator
          orientation="vertical"
          className="mr-2 data-[orientation=vertical]:h-4"
        />
        <Breadcrumb>
          <BreadcrumbList>
            <BreadcrumbItem className="hidden md:block">
              <BreadcrumbLink href="#">
                Orders Management
              </BreadcrumbLink>
            </BreadcrumbItem>
            <BreadcrumbSeparator className="hidden md:block" />
            <BreadcrumbItem>
              <BreadcrumbPage>
                {status !== undefined ? getStatusDisplayName(status) : 'All Orders'}
              </BreadcrumbPage>
            </BreadcrumbItem>
          </BreadcrumbList>
        </Breadcrumb>
      </header>
        <div className="flex flex-1 flex-col gap-4 p-4">
        <div className="flex flex-col gap-3">
          <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
            <h1 className="text-2xl font-bold">
              {status !== undefined ? getStatusDisplayName(status) : 'All Orders'}
              {data && (
                <span className="text-lg font-normal text-muted-foreground ml-2">
                  ({data.totalCount} {data.totalCount === 1 ? 'order' : 'orders'})
                </span>
              )}
            </h1>
            <div className="flex items-center gap-2">
              {canCreateOrder ? (
                <Button
                  size="sm"
                  onClick={handleNewOrderClick}
                  data-testid="new-order-button"
                >
                  <Plus className="w-4 h-4 mr-2" />
                  New Order
                </Button>
              ) : (
                <Tooltip>
                  <TooltipTrigger asChild>
                    <span tabIndex={0} data-testid="new-order-button-disabled-wrapper">
                      <Button
                        size="sm"
                        disabled
                        data-testid="new-order-button"
                        aria-describedby="new-order-disabled-help"
                      >
                        <Plus className="w-4 h-4 mr-2" />
                        New Order
                      </Button>
                    </span>
                  </TooltipTrigger>
                  <TooltipContent sideOffset={8}>
                    Requires <code>orders.write</code> or <code>orders.manage</code>.
                  </TooltipContent>
                </Tooltip>
              )}
              <Button 
                variant="outline" 
                size="sm" 
                onClick={() => refetch()}
                disabled={loading}
              >
                <RefreshCw className="w-4 h-4 mr-2" />
                Refresh
              </Button>
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-2" data-testid="status-filter-group">
            {STATUS_FILTERS.map((filter) => {
              const isSelected = filter.value === undefined
                ? status === undefined
                : status === filter.value;

              return (
                <Button
                  key={filter.label}
                  type="button"
                  size="sm"
                  variant={isSelected ? 'default' : 'outline'}
                  data-testid={`status-filter-${filter.label.toLowerCase().replace(/\s+/g, '-')}`}
                  onClick={() => handleStatusFilterChange(filter.value)}
                >
                  {filter.label}
                </Button>
              );
            })}
          </div>
        </div>
        
        {data && data.orders.length > 0 ? (
          <OrdersTable 
            data={data.orders}
            isAdmin={isAdmin}
            onViewOrder={handleViewOrder}
            onCancelOrder={handleCancelOrder}
            onAddTracking={handleAddTracking}
          />
        ) : (
          <div className="bg-muted/50 rounded-xl p-8 text-center">
            <p className="text-muted-foreground">
              {status !== undefined 
                ? `No ${getStatusDisplayName(status).toLowerCase()} found.`
                : 'No orders found.'
              }
            </p>
          </div>
        )}

        {data && data.totalPages > 1 && (
          <div className="flex items-center justify-between mt-4">
            <p className="text-sm text-muted-foreground">
              Showing {((page - 1) * pageSize) + 1} to {Math.min(page * pageSize, data.totalCount)} of {data.totalCount} orders
            </p>
            {/* Pagination controls would go here */}
          </div>
        )}
      </div>
    </>
  );
}

function getStatusDisplayName(status: OrderStatus): string {
  switch (status) {
    case OrderStatus.Draft: return 'Draft Orders';
    case OrderStatus.Confirmed: return 'Confirmed Orders';
    case OrderStatus.Processing: return 'Processing Orders';
    case OrderStatus.Shipped: return 'Shipped Orders';
    case OrderStatus.Delivered: return 'Delivered Orders';
    case OrderStatus.Cancelled: return 'Cancelled Orders';
    default: return 'Orders';
  }
}
