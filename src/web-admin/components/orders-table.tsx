'use client';

import { useMemo } from 'react';
import { MoreHorizontal, Eye, XCircle, Truck } from 'lucide-react';
import { ColumnDef } from '@tanstack/react-table';
import {
  TableProvider,
  TableHeader,
  TableHeaderGroup,
  TableHead,
  TableBody,
  TableRow,
  TableCell,
  TableColumnHeader,
} from '@/components/ui/data-table';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { OrderStatusBadge } from '@/components/order-status-badge';
import { OrderSummary } from '@/types/orders';

interface OrdersTableProps {
  data: OrderSummary[];
  isAdmin?: boolean;
  onViewOrder: (orderId: number) => void;
  onCancelOrder: (orderId: number) => void;
  onAddTracking: (orderId: number) => void;
}

export function OrdersTable({
  data,
  isAdmin = false,
  onViewOrder,
  onCancelOrder,
  onAddTracking,
}: OrdersTableProps) {
  const columns = useMemo<ColumnDef<OrderSummary>[]>(() => [
    {
      accessorKey: 'orderNumber',
      header: ({ column }) => (
        <TableColumnHeader column={column} title="Order #" />
      ),
      cell: ({ row }) => (
        <div className="font-medium">
          {row.getValue('orderNumber')}
        </div>
      ),
    },
    {
      accessorKey: 'status',
      header: ({ column }) => (
        <TableColumnHeader column={column} title="Status" />
      ),
      cell: ({ row }) => (
        <OrderStatusBadge status={row.getValue('status')} />
      ),
    },
    {
      accessorKey: 'customerName',
      header: ({ column }) => (
        <TableColumnHeader column={column} title="Customer" />
      ),
      cell: ({ row }) => (
        <div>
          <div className="font-medium">{row.getValue('customerName')}</div>
          <div className="text-sm text-muted-foreground">
            {row.original.customerEmail}
          </div>
        </div>
      ),
    },
    {
      accessorKey: 'totalAmount',
      header: ({ column }) => (
        <TableColumnHeader column={column} title="Total" />
      ),
      cell: ({ row }) => {
        const amount = parseFloat(row.getValue('totalAmount'));
        const currency = row.original.currency || 'USD';
        
        return (
          <div className="font-medium">
            {new Intl.NumberFormat('en-US', {
              style: 'currency',
              currency,
            }).format(amount)}
          </div>
        );
      },
    },
    {
      accessorKey: 'itemCount',
      header: ({ column }) => (
        <TableColumnHeader column={column} title="Items" />
      ),
      cell: ({ row }) => (
        <div className="text-center">
          {row.getValue('itemCount')}
        </div>
      ),
    },
    {
      accessorKey: 'createdAt',
      header: ({ column }) => (
        <TableColumnHeader column={column} title="Date" />
      ),
      cell: ({ row }) => {
        const date = new Date(row.getValue('createdAt'));
        return (
          <div>
            <div className="font-medium">
              {date.toLocaleDateString('en-US', {
                month: 'short',
                day: 'numeric',
                year: 'numeric',
              })}
            </div>
            <div className="text-sm text-muted-foreground">
              {date.toLocaleTimeString('en-US', {
                hour: '2-digit',
                minute: '2-digit',
              })}
            </div>
          </div>
        );
      },
    },
    {
      id: 'actions',
      enableHiding: false,
      cell: ({ row }) => {
        const order = row.original;

        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" className="h-8 w-8 p-0">
                <span className="sr-only">Open menu</span>
                <MoreHorizontal className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem
                onClick={() => onViewOrder(order.id)}
                className="cursor-pointer"
              >
                <Eye className="mr-2 h-4 w-4" />
                View Details
              </DropdownMenuItem>
              
              {order.canBeCancelled && (
                <>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    onClick={() => onCancelOrder(order.id)}
                    className="cursor-pointer text-red-600"
                  >
                    <XCircle className="mr-2 h-4 w-4" />
                    Cancel Order
                  </DropdownMenuItem>
                </>
              )}
              
              {isAdmin && (
                <>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    onClick={() => onAddTracking(order.id)}
                    className="cursor-pointer"
                  >
                    <Truck className="mr-2 h-4 w-4" />
                    Add Tracking
                  </DropdownMenuItem>
                </>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        );
      },
    },
  ], [isAdmin, onViewOrder, onCancelOrder, onAddTracking]);

  return (
    <div className="w-full">
      <TableProvider data={data} columns={columns}>
        <TableHeader>
          {({ headerGroup }) => (
            <TableHeaderGroup headerGroup={headerGroup}>
              {({ header }) => <TableHead key={header.id} header={header} />}
            </TableHeaderGroup>
          )}
        </TableHeader>
        <TableBody>
          {({ row }) => (
            <TableRow key={row.id} row={row}>
              {({ cell }) => (
                <TableCell key={cell.id} cell={cell} />
              )}
            </TableRow>
          )}
        </TableBody>
      </TableProvider>
    </div>
  );
}