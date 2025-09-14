'use client';

import { useParams, useRouter } from 'next/navigation';
import { useMemo } from 'react';
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
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Skeleton } from "@/components/ui/skeleton"
import { OrderStatusBadge } from "@/components/order-status-badge"
import { useOrder } from "@/hooks/use-order"
import { ordersApi } from "@/lib/orders-api"
import { OrderStatus } from "@/types/orders"
import { ArrowLeft, Package, User, CreditCard, Truck } from "lucide-react"

export default function OrderDetailPage() {
  const params = useParams();
  const router = useRouter();
  
  const orderId = parseInt(params.id as string);
  const { data: order, loading, error, refetch } = useOrder(orderId);
  const isAdmin = useMemo(() => ordersApi.isAdmin(), []);

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
                <BreadcrumbLink href="/orders">Orders Management</BreadcrumbLink>
              </BreadcrumbItem>
              <BreadcrumbSeparator className="hidden md:block" />
              <BreadcrumbItem>
                <BreadcrumbPage>Loading...</BreadcrumbPage>
              </BreadcrumbItem>
            </BreadcrumbList>
          </Breadcrumb>
        </header>
        <div className="flex flex-1 flex-col gap-4 p-4">
          <div className="flex items-center gap-2">
            <Skeleton className="h-9 w-20" />
            <Skeleton className="h-8 w-32" />
          </div>
          <div className="grid gap-4 md:grid-cols-2">
            <Skeleton className="h-64" />
            <Skeleton className="h-64" />
          </div>
        </div>
      </>
    );
  }

  if (error || !order) {
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
                <BreadcrumbLink href="/orders">Orders Management</BreadcrumbLink>
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
            <p className="text-destructive font-medium">Failed to load order</p>
            <p className="text-destructive/80 text-sm mt-1">
              {error || 'Order not found'}
            </p>
            <div className="flex gap-2 mt-3">
              <Button 
                variant="outline" 
                size="sm" 
                onClick={() => router.back()}
              >
                <ArrowLeft className="w-4 h-4 mr-2" />
                Go Back
              </Button>
              <Button 
                variant="outline" 
                size="sm" 
                onClick={() => refetch()}
              >
                Try Again
              </Button>
            </div>
          </div>
        </div>
      </>
    );
  }

  const handleCancel = async () => {
    if (!confirm('Are you sure you want to cancel this order?')) return;
    
    try {
      await ordersApi.cancelOrder(orderId, 'Cancelled by admin');
      await refetch();
    } catch (error) {
      console.error('Failed to cancel order:', error);
    }
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
              <BreadcrumbLink href="/orders">Orders Management</BreadcrumbLink>
            </BreadcrumbItem>
            <BreadcrumbSeparator className="hidden md:block" />
            <BreadcrumbItem>
              <BreadcrumbPage>Order #{order.orderNumber}</BreadcrumbPage>
            </BreadcrumbItem>
          </BreadcrumbList>
        </Breadcrumb>
      </header>
      
      <div className="flex flex-1 flex-col gap-6 p-4">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Button 
              variant="ghost" 
              size="sm" 
              onClick={() => router.back()}
            >
              <ArrowLeft className="w-4 h-4 mr-2" />
              Back
            </Button>
            <div>
              <h1 className="text-2xl font-bold">Order #{order.orderNumber}</h1>
              <p className="text-muted-foreground">
                Created {new Date(order.createdAt).toLocaleDateString()}
              </p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <OrderStatusBadge status={order.status} />
            {isAdmin && order.canBeCancelled && (
              <Button variant="destructive" size="sm" onClick={handleCancel}>
                Cancel Order
              </Button>
            )}
          </div>
        </div>

        <div className="grid gap-6 md:grid-cols-2">
          {/* Customer Information */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <User className="w-5 h-5" />
                Customer Information
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              <div>
                <p className="font-medium">{order.customer.name}</p>
                <p className="text-sm text-muted-foreground">{order.customer.email}</p>
                {order.customer.phone && (
                  <p className="text-sm text-muted-foreground">{order.customer.phone}</p>
                )}
              </div>
              {order.customer.address && (
                <div>
                  <p className="text-sm font-medium">Address:</p>
                  <p className="text-sm text-muted-foreground">
                    {order.customer.address}
                    {order.customer.city && `, ${order.customer.city}`}
                    {order.customer.state && `, ${order.customer.state}`}
                    {order.customer.postalCode && ` ${order.customer.postalCode}`}
                    {order.customer.country && `, ${order.customer.country}`}
                  </p>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Order Summary */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <CreditCard className="w-5 h-5" />
                Order Summary
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex justify-between">
                <span>Subtotal:</span>
                <span>${order.subtotalAmount.toFixed(2)}</span>
              </div>
              <div className="flex justify-between">
                <span>Tax:</span>
                <span>${order.taxAmount.toFixed(2)}</span>
              </div>
              <div className="flex justify-between">
                <span>Shipping:</span>
                <span>${order.shippingAmount.toFixed(2)}</span>
              </div>
              {order.discountAmount > 0 && (
                <div className="flex justify-between text-green-600">
                  <span>Discount:</span>
                  <span>-${order.discountAmount.toFixed(2)}</span>
                </div>
              )}
              <Separator />
              <div className="flex justify-between font-bold">
                <span>Total:</span>
                <span>${order.totalAmount.toFixed(2)} {order.currency}</span>
              </div>
              {order.paymentMethod && (
                <p className="text-sm text-muted-foreground">
                  Payment: {order.paymentMethod}
                </p>
              )}
            </CardContent>
          </Card>

          {/* Shipping Information */}
          {order.shipping && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Truck className="w-5 h-5" />
                  Shipping Information
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-2">
                <div>
                  <p className="font-medium">Method: {order.shipping.method}</p>
                  {order.shipping.carrier && (
                    <p className="text-sm text-muted-foreground">
                      Carrier: {order.shipping.carrier}
                    </p>
                  )}
                  {order.trackingNumber && (
                    <div>
                      <p className="text-sm font-medium">Tracking Number:</p>
                      <p className="text-sm font-mono">{order.trackingNumber}</p>
                    </div>
                  )}
                </div>
                <div>
                  <p className="text-sm font-medium">Delivery Address:</p>
                  <p className="text-sm text-muted-foreground">
                    {order.shipping.address}, {order.shipping.city}, {order.shipping.state} {order.shipping.postalCode}, {order.shipping.country}
                  </p>
                </div>
              </CardContent>
            </Card>
          )}

          {/* Order Items */}
          <Card className="md:col-span-2">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Package className="w-5 h-5" />
                Order Items ({order.itemCount} items)
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {order.items.map((item) => (
                  <div key={item.id} className="flex items-center justify-between p-3 border rounded-lg">
                    <div className="flex-1">
                      <p className="font-medium">{item.productName}</p>
                      {item.sku && (
                        <p className="text-sm text-muted-foreground">SKU: {item.sku}</p>
                      )}
                      {item.description && (
                        <p className="text-sm text-muted-foreground">{item.description}</p>
                      )}
                    </div>
                    <div className="text-right">
                      <p className="font-medium">${item.totalPrice.toFixed(2)}</p>
                      <p className="text-sm text-muted-foreground">
                        {item.quantity} × ${item.unitPrice.toFixed(2)}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>

          {/* Status History */}
          {order.statusHistory.length > 0 && (
            <Card className="md:col-span-2">
              <CardHeader>
                <CardTitle>Status History</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  {order.statusHistory.map((history) => (
                    <div key={history.id} className="flex items-center justify-between p-3 border rounded-lg">
                      <div className="flex items-center gap-3">
                        <OrderStatusBadge status={history.status} />
                        <div>
                          <p className="font-medium">{history.statusDisplay}</p>
                          {history.notes && (
                            <p className="text-sm text-muted-foreground">{history.notes}</p>
                          )}
                        </div>
                      </div>
                      <div className="text-right text-sm text-muted-foreground">
                        <p>{new Date(history.createdAt).toLocaleDateString()}</p>
                        <p>by {history.createdBy}</p>
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </>
  );
}