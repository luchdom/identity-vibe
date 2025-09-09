import { 
  Eye, 
  ChevronLeft, 
  ChevronRight, 
  ChevronsLeft, 
  ChevronsRight,
  MoreHorizontal,
  FileText,
  Ban
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { 
  Card, 
  CardContent, 
  CardHeader, 
  CardTitle 
} from '@/components/ui/card';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import type { OrderSummaryResponse, OrderListResponse } from '@/types/orders';
import { OrderStatusBadge } from './OrderStatusBadge';
import { format } from 'date-fns';

interface OrdersTableProps {
  data: OrderListResponse | null;
  isLoading: boolean;
  onViewOrder: (orderId: number) => void;
  onCancelOrder: (orderId: number) => void;
  onPageChange: (page: number) => void;
  currentPage: number;
}

export function OrdersTable({ 
  data, 
  isLoading, 
  onViewOrder, 
  onCancelOrder, 
  onPageChange
}: OrdersTableProps) {
  const formatCurrency = (amount: number, currency: string = 'USD') => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency,
    }).format(amount);
  };

  const formatDate = (dateString: string) => {
    return format(new Date(dateString), 'MMM dd, yyyy');
  };

  const formatDateTime = (dateString: string) => {
    return format(new Date(dateString), 'MMM dd, yyyy HH:mm');
  };

  const renderLoadingSkeleton = () => (
    <div className="space-y-3">
      {Array.from({ length: 5 }).map((_, i) => (
        <div key={i} className="flex items-center space-x-4 p-4">
          <Skeleton className="h-4 w-[100px]" />
          <Skeleton className="h-4 w-[150px]" />
          <Skeleton className="h-4 w-[80px]" />
          <Skeleton className="h-4 w-[120px]" />
          <Skeleton className="h-4 w-[100px]" />
          <Skeleton className="h-4 w-[80px]" />
        </div>
      ))}
    </div>
  );

  const renderEmptyState = () => (
    <div className="text-center py-12">
      <FileText className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
      <h3 className="text-lg font-semibold mb-2">No orders found</h3>
      <p className="text-muted-foreground mb-4">
        There are no orders matching your criteria.
      </p>
    </div>
  );

  const renderPagination = () => {
    if (!data || data.totalPages <= 1) return null;

    const { pageNumber, totalPages, hasPreviousPage, hasNextPage } = data;

    return (
      <div className="flex items-center justify-between px-4 py-3 border-t">
        <div className="text-sm text-muted-foreground">
          Showing page {pageNumber} of {totalPages} ({data.totalCount} total orders)
        </div>
        
        <div className="flex items-center space-x-1">
          <Button
            variant="outline"
            size="sm"
            onClick={() => onPageChange(1)}
            disabled={!hasPreviousPage}
          >
            <ChevronsLeft className="h-4 w-4" />
          </Button>
          
          <Button
            variant="outline"
            size="sm"
            onClick={() => onPageChange(pageNumber - 1)}
            disabled={!hasPreviousPage}
          >
            <ChevronLeft className="h-4 w-4" />
          </Button>
          
          <div className="flex items-center space-x-1">
            {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
              let pageNum;
              if (totalPages <= 5) {
                pageNum = i + 1;
              } else if (pageNumber <= 3) {
                pageNum = i + 1;
              } else if (pageNumber >= totalPages - 2) {
                pageNum = totalPages - 4 + i;
              } else {
                pageNum = pageNumber - 2 + i;
              }

              if (pageNum < 1 || pageNum > totalPages) return null;

              return (
                <Button
                  key={pageNum}
                  variant={pageNum === pageNumber ? "default" : "outline"}
                  size="sm"
                  onClick={() => onPageChange(pageNum)}
                >
                  {pageNum}
                </Button>
              );
            })}
          </div>
          
          <Button
            variant="outline"
            size="sm"
            onClick={() => onPageChange(pageNumber + 1)}
            disabled={!hasNextPage}
          >
            <ChevronRight className="h-4 w-4" />
          </Button>
          
          <Button
            variant="outline"
            size="sm"
            onClick={() => onPageChange(totalPages)}
            disabled={!hasNextPage}
          >
            <ChevronsRight className="h-4 w-4" />
          </Button>
        </div>
      </div>
    );
  };

  const renderActionsDropdown = (order: OrderSummaryResponse) => (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="sm">
          <MoreHorizontal className="h-4 w-4" />
          <span className="sr-only">Open actions menu</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem onClick={() => onViewOrder(order.id)}>
          <Eye className="h-4 w-4 mr-2" />
          View Details
        </DropdownMenuItem>
        
        {order.canBeCancelled && (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuItem 
              onClick={() => onCancelOrder(order.id)}
              className="text-destructive focus:text-destructive"
            >
              <Ban className="h-4 w-4 mr-2" />
              Cancel Order
            </DropdownMenuItem>
          </>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Orders</CardTitle>
        </CardHeader>
        <CardContent>
          {renderLoadingSkeleton()}
        </CardContent>
      </Card>
    );
  }

  if (!data || data.orders.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Orders</CardTitle>
        </CardHeader>
        <CardContent>
          {renderEmptyState()}
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Orders</CardTitle>
      </CardHeader>
      <CardContent className="p-0">
        <div className="overflow-x-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Order #</TableHead>
                <TableHead>Customer</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Items</TableHead>
                <TableHead>Total</TableHead>
                <TableHead>Date</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.orders.map((order) => (
                <TableRow key={order.id}>
                  <TableCell className="font-medium">
                    <div className="flex flex-col">
                      <span className="font-mono text-sm">{order.orderNumber}</span>
                      <span className="text-xs text-muted-foreground">
                        ID: {order.id}
                      </span>
                    </div>
                  </TableCell>
                  
                  <TableCell>
                    <div className="flex flex-col">
                      <span className="font-medium">{order.customerName}</span>
                      <span className="text-sm text-muted-foreground">
                        {order.customerEmail}
                      </span>
                    </div>
                  </TableCell>
                  
                  <TableCell>
                    <OrderStatusBadge status={order.status} />
                  </TableCell>
                  
                  <TableCell>
                    <Badge variant="secondary" className="font-mono">
                      {order.itemCount} {order.itemCount === 1 ? 'item' : 'items'}
                    </Badge>
                  </TableCell>
                  
                  <TableCell className="font-medium">
                    {formatCurrency(order.totalAmount, order.currency)}
                  </TableCell>
                  
                  <TableCell>
                    <div className="flex flex-col">
                      <span className="text-sm">{formatDate(order.createdAt)}</span>
                      <span className="text-xs text-muted-foreground">
                        {formatDateTime(order.updatedAt)}
                      </span>
                    </div>
                  </TableCell>
                  
                  <TableCell className="text-right">
                    <div className="flex items-center justify-end space-x-1">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => onViewOrder(order.id)}
                      >
                        <Eye className="h-4 w-4" />
                        <span className="sr-only">View order</span>
                      </Button>
                      
                      {renderActionsDropdown(order)}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
        
        {renderPagination()}
      </CardContent>
    </Card>
  );
}