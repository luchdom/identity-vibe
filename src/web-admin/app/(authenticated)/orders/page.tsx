'use client';

import { useSearchParams, useRouter } from 'next/navigation';
import { useState, useMemo } from 'react';
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
import { useOrdersWithStatus } from "@/hooks/use-orders"
import { ordersApi } from "@/lib/orders-api"
import { OrderStatus } from "@/types/orders"
import { RefreshCw } from "lucide-react"

export default function OrdersPage() {
  const searchParams = useSearchParams();
  const router = useRouter();
  
  // Get status from URL params
  const statusParam = searchParams.get('status');
  const pageParam = searchParams.get('page');
  
  const status = statusParam ? parseInt(statusParam) as OrderStatus : undefined;
  const page = pageParam ? parseInt(pageParam) : 1;
  const pageSize = 20;

  const { data, loading, error, refetch } = useOrdersWithStatus(status, page, pageSize);
  const isAdmin = useMemo(() => ordersApi.isAdmin(), []);

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
          <h1 className="text-2xl font-bold">
            {status !== undefined ? getStatusDisplayName(status) : 'All Orders'}
            {data && (
              <span className="text-lg font-normal text-muted-foreground ml-2">
                ({data.totalCount} {data.totalCount === 1 ? 'order' : 'orders'})
              </span>
            )}
          </h1>
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