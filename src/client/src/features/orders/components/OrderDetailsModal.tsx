import { 
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { ScrollArea } from '@/components/ui/scroll-area';
import { 
  Package, 
  User, 
  Truck, 
  Clock,
  Ban,
  Edit,
  X
} from 'lucide-react';
import { OrderStatus } from '@/types/orders';
import type { OrderResponse, UpdateOrderStatusRequest } from '@/types/orders';
import { OrderStatusBadge } from './OrderStatusBadge';
import { format } from 'date-fns';

interface OrderDetailsModalProps {
  orderId: number | null;
  order: OrderResponse | null;
  isLoading: boolean;
  isOpen: boolean;
  onClose: () => void;
  onCancelOrder?: (orderId: number, reason?: string) => void;
  onUpdateStatus?: (orderId: number, statusUpdate: UpdateOrderStatusRequest) => void;
  isAdmin?: boolean;
}

export function OrderDetailsModal({ 
  order, 
  isLoading, 
  isOpen, 
  onClose,
  onCancelOrder,
  onUpdateStatus,
  isAdmin = false
}: OrderDetailsModalProps) {
  const formatCurrency = (amount: number, currency: string = 'USD') => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency,
    }).format(amount);
  };

  const formatDateTime = (dateString: string) => {
    return format(new Date(dateString), 'PPpp');
  };

  const formatDate = (dateString: string) => {
    return format(new Date(dateString), 'PP');
  };

  const handleCancelOrder = () => {
    if (order && onCancelOrder) {
      onCancelOrder(order.id);
    }
  };

  const renderLoadingSkeleton = () => (
    <div className="space-y-4">
      <div className="h-4 bg-muted animate-pulse rounded"></div>
      <div className="h-4 bg-muted animate-pulse rounded w-3/4"></div>
      <div className="h-4 bg-muted animate-pulse rounded w-1/2"></div>
    </div>
  );

  const renderStatusHistory = () => {
    if (!order?.statusHistory?.length) return null;

    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center text-sm">
            <Clock className="h-4 w-4 mr-2" />
            Status History
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          {order.statusHistory.map((history) => (
            <div key={history.id} className="flex items-start space-x-3">
              <div className="flex-shrink-0 w-2 h-2 bg-primary rounded-full mt-2"></div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center justify-between">
                  <OrderStatusBadge status={history.status} />
                  <span className="text-xs text-muted-foreground">
                    {formatDateTime(history.timestamp)}
                  </span>
                </div>
                {history.note && (
                  <p className="text-sm text-muted-foreground mt-1">
                    {history.note}
                  </p>
                )}
                {history.changedBy && (
                  <p className="text-xs text-muted-foreground mt-1">
                    Changed by: {history.changedBy}
                  </p>
                )}
              </div>
            </div>
          ))}
        </CardContent>
      </Card>
    );
  };

  const renderOrderItems = () => {
    if (!order?.items?.length) return null;

    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center text-sm">
            <Package className="h-4 w-4 mr-2" />
            Order Items ({order.itemCount})
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {order.items.map((item) => (
            <div key={item.id} className="flex items-start space-x-4">
              {item.imageUrl && (
                <img 
                  src={item.imageUrl} 
                  alt={item.productName}
                  className="w-12 h-12 object-cover rounded border"
                />
              )}
              <div className="flex-1 min-w-0">
                <h4 className="font-medium text-sm">{item.productName}</h4>
                <p className="text-xs text-muted-foreground">
                  Product ID: {item.productId}
                </p>
                <div className="flex items-center justify-between mt-1">
                  <span className="text-sm">
                    Qty: {item.quantity} × {formatCurrency(item.unitPrice, order.currency)}
                  </span>
                  <span className="font-medium text-sm">
                    {formatCurrency(item.totalPrice, order.currency)}
                  </span>
                </div>
              </div>
            </div>
          ))}
          
          {/* Order totals */}
          <Separator />
          <div className="space-y-2">
            <div className="flex justify-between text-sm">
              <span>Subtotal:</span>
              <span>{formatCurrency(order.subtotalAmount, order.currency)}</span>
            </div>
            {order.taxAmount > 0 && (
              <div className="flex justify-between text-sm">
                <span>Tax:</span>
                <span>{formatCurrency(order.taxAmount, order.currency)}</span>
              </div>
            )}
            {order.shippingAmount > 0 && (
              <div className="flex justify-between text-sm">
                <span>Shipping:</span>
                <span>{formatCurrency(order.shippingAmount, order.currency)}</span>
              </div>
            )}
            {order.discountAmount > 0 && (
              <div className="flex justify-between text-sm text-green-600">
                <span>Discount:</span>
                <span>-{formatCurrency(order.discountAmount, order.currency)}</span>
              </div>
            )}
            <Separator />
            <div className="flex justify-between font-medium">
              <span>Total:</span>
              <span>{formatCurrency(order.totalAmount, order.currency)}</span>
            </div>
          </div>
        </CardContent>
      </Card>
    );
  };

  const renderCustomerInfo = () => {
    if (!order?.customer) return null;

    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center text-sm">
            <User className="h-4 w-4 mr-2" />
            Customer Information
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-2">
          <div>
            <p className="font-medium">{order.customer.name}</p>
            <p className="text-sm text-muted-foreground">{order.customer.email}</p>
          </div>
          {order.customer.phone && (
            <div>
              <p className="text-sm text-muted-foreground">
                Phone: {order.customer.phone}
              </p>
            </div>
          )}
          {order.customer.address && (
            <div>
              <p className="text-xs text-muted-foreground mb-1">Address:</p>
              <p className="text-sm">{order.customer.address}</p>
            </div>
          )}
        </CardContent>
      </Card>
    );
  };

  const renderShippingInfo = () => {
    if (!order) return null;

    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center text-sm">
            <Truck className="h-4 w-4 mr-2" />
            Shipping Information
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-2">
          {order.trackingNumber && (
            <div>
              <p className="text-xs text-muted-foreground mb-1">Tracking Number:</p>
              <p className="font-mono text-sm">{order.trackingNumber}</p>
            </div>
          )}
          {order.shippedAt && (
            <div>
              <p className="text-xs text-muted-foreground mb-1">Shipped Date:</p>
              <p className="text-sm">{formatDateTime(order.shippedAt)}</p>
            </div>
          )}
          {order.deliveredAt && (
            <div>
              <p className="text-xs text-muted-foreground mb-1">Delivered Date:</p>
              <p className="text-sm">{formatDateTime(order.deliveredAt)}</p>
            </div>
          )}
        </CardContent>
      </Card>
    );
  };

  const renderActions = () => {
    if (!order) return null;

    return (
      <div className="flex items-center space-x-2 pt-4 border-t">
        {order.canBeCancelled && onCancelOrder && (
          <Button 
            variant="outline" 
            size="sm"
            onClick={handleCancelOrder}
            className="text-destructive border-destructive hover:bg-destructive hover:text-destructive-foreground"
          >
            <Ban className="h-4 w-4 mr-2" />
            Cancel Order
          </Button>
        )}
        
        {isAdmin && onUpdateStatus && order.status !== OrderStatus.Delivered && order.status !== OrderStatus.Cancelled && (
          <Button variant="outline" size="sm">
            <Edit className="h-4 w-4 mr-2" />
            Update Status
          </Button>
        )}
        
        <div className="flex-1"></div>
        
        <Button variant="outline" size="sm" onClick={onClose}>
          <X className="h-4 w-4 mr-2" />
          Close
        </Button>
      </div>
    );
  };

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-4xl max-h-[90vh] p-0">
        <DialogHeader className="p-6 pb-0">
          <DialogTitle>
            {order ? `Order ${order.orderNumber}` : `Order Details`}
          </DialogTitle>
        </DialogHeader>
        
        <ScrollArea className="px-6 pb-6">
          {isLoading && renderLoadingSkeleton()}
          
          {order && (
            <div className="space-y-6">
              {/* Order Header */}
              <div className="flex items-center justify-between">
                <div>
                  <div className="flex items-center space-x-3 mb-2">
                    <OrderStatusBadge status={order.status} />
                    <span className="text-sm text-muted-foreground">
                      Created {formatDate(order.createdAt)}
                    </span>
                  </div>
                  <div className="text-xs text-muted-foreground">
                    Order ID: {order.id} | Last updated: {formatDateTime(order.updatedAt)}
                  </div>
                </div>
                <div className="text-right">
                  <div className="text-2xl font-bold">
                    {formatCurrency(order.totalAmount, order.currency)}
                  </div>
                  <div className="text-sm text-muted-foreground">
                    {order.itemCount} {order.itemCount === 1 ? 'item' : 'items'}
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div className="space-y-6">
                  {renderCustomerInfo()}
                  {renderShippingInfo()}
                </div>
                <div className="space-y-6">
                  {renderOrderItems()}
                </div>
              </div>
              
              {renderStatusHistory()}
              {renderActions()}
            </div>
          )}
        </ScrollArea>
      </DialogContent>
    </Dialog>
  );
}