'use client';

import { useMemo, useState } from 'react';
import { useRouter } from 'next/navigation';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import type { CreateOrderItem, CreateOrderRequest } from '@/types/orders';
import { ordersApi } from '@/lib/orders-api';

interface FormErrors {
  customerName?: string;
  customerEmail?: string;
  items?: string;
}

interface NewOrderFormProps {
  onCancel?: () => void;
}

function createEmptyItem(): CreateOrderItem {
  return {
    productName: '',
    quantity: 1,
    unitPrice: 0,
    sku: '',
    description: '',
  };
}

export function NewOrderForm({ onCancel }: NewOrderFormProps) {
  const router = useRouter();
  const [submitting, setSubmitting] = useState(false);
  const [errors, setErrors] = useState<FormErrors>({});

  const [customerName, setCustomerName] = useState('');
  const [customerEmail, setCustomerEmail] = useState('');
  const [customerPhone, setCustomerPhone] = useState('');
  const [address, setAddress] = useState('');
  const [city, setCity] = useState('');
  const [stateRegion, setStateRegion] = useState('');
  const [postalCode, setPostalCode] = useState('');
  const [country, setCountry] = useState('');
  const [includeShipping, setIncludeShipping] = useState(false);
  const [shippingMethod, setShippingMethod] = useState('');
  const [paymentMethod, setPaymentMethod] = useState('');
  const [specialInstructions, setSpecialInstructions] = useState('');
  const [items, setItems] = useState<CreateOrderItem[]>([createEmptyItem()]);

  const itemTotal = useMemo(
    () => items.reduce((sum, item) => sum + (Number(item.quantity) || 0) * (Number(item.unitPrice) || 0), 0),
    [items]
  );

  const updateItem = (index: number, patch: Partial<CreateOrderItem>) => {
    setItems((current) => current.map((item, i) => (i === index ? { ...item, ...patch } : item)));
  };

  const addItem = () => setItems((current) => [...current, createEmptyItem()]);

  const removeItem = (index: number) => {
    setItems((current) => (current.length > 1 ? current.filter((_, i) => i !== index) : current));
  };

  const validate = (): boolean => {
    const nextErrors: FormErrors = {};

    if (!customerName.trim()) {
      nextErrors.customerName = 'Customer name is required.';
    }

    if (!customerEmail.trim()) {
      nextErrors.customerEmail = 'Customer email is required.';
    }

    const validItems = items.filter(
      (item) => item.productName.trim() && Number(item.quantity) > 0 && Number(item.unitPrice) >= 0
    );

    if (validItems.length === 0) {
      nextErrors.items = 'At least one item with product name, quantity and unit price is required.';
    }

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleSubmit = async () => {
    if (!validate()) {
      return;
    }

    setSubmitting(true);
    try {
      const payload: CreateOrderRequest = {
        customer: {
          name: customerName.trim(),
          email: customerEmail.trim(),
          phone: customerPhone.trim() || undefined,
          address: address.trim() || undefined,
          city: city.trim() || undefined,
          state: stateRegion.trim() || undefined,
          postalCode: postalCode.trim() || undefined,
          country: country.trim() || undefined,
        },
        items: items
          .filter((item) => item.productName.trim())
          .map((item) => ({
            productName: item.productName.trim(),
            sku: item.sku?.trim() || undefined,
            quantity: Number(item.quantity),
            unitPrice: Number(item.unitPrice),
            description: item.description?.trim() || undefined,
          })),
        paymentMethod: paymentMethod.trim() || undefined,
        specialInstructions: specialInstructions.trim() || undefined,
        shipping: includeShipping
          ? {
              method: shippingMethod.trim() || 'Standard',
              address: address.trim() || '',
              city: city.trim() || '',
              state: stateRegion.trim() || '',
              postalCode: postalCode.trim() || '',
              country: country.trim() || '',
            }
          : undefined,
      };

      await ordersApi.createOrder(payload);
      toast.success('Order created successfully');
      router.push('/orders?created=1');
    } catch (error: unknown) {
      let message = 'Failed to create order';
      if (typeof error === 'object' && error !== null) {
        const maybeError = error as { response?: { data?: { detail?: string; message?: string } }; message?: string };
        message = maybeError.response?.data?.detail || maybeError.response?.data?.message || maybeError.message || message;
      }

      toast.error(message);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="space-y-6" data-testid="new-order-form">
      <section className="rounded-xl border p-4 space-y-3">
        <h2 className="text-lg font-semibold">Customer</h2>
        <div className="grid gap-3 md:grid-cols-2">
          <div className="space-y-1">
            <label className="text-sm font-medium">Name *</label>
            <input data-testid="customer-name" className="h-9 w-full rounded-md border px-3 text-sm" value={customerName} onChange={(e) => setCustomerName(e.target.value)} />
            {errors.customerName ? <p className="text-xs text-red-600">{errors.customerName}</p> : null}
          </div>
          <div className="space-y-1">
            <label className="text-sm font-medium">Email *</label>
            <input data-testid="customer-email" type="email" className="h-9 w-full rounded-md border px-3 text-sm" value={customerEmail} onChange={(e) => setCustomerEmail(e.target.value)} />
            {errors.customerEmail ? <p className="text-xs text-red-600">{errors.customerEmail}</p> : null}
          </div>
          <div className="space-y-1">
            <label className="text-sm font-medium">Phone</label>
            <input className="h-9 w-full rounded-md border px-3 text-sm" value={customerPhone} onChange={(e) => setCustomerPhone(e.target.value)} />
          </div>
          <div className="space-y-1">
            <label className="text-sm font-medium">Payment Method</label>
            <input className="h-9 w-full rounded-md border px-3 text-sm" value={paymentMethod} onChange={(e) => setPaymentMethod(e.target.value)} placeholder="Card, Invoice, ..." />
          </div>
        </div>
      </section>

      <section className="rounded-xl border p-4 space-y-3">
        <div className="flex items-center justify-between gap-2">
          <h2 className="text-lg font-semibold">Items</h2>
          <Button type="button" variant="outline" size="sm" onClick={addItem} data-testid="add-order-item-button">Add Item</Button>
        </div>
        {errors.items ? <p className="text-xs text-red-600">{errors.items}</p> : null}
        <div className="space-y-3">
          {items.map((item, index) => (
            <div key={index} className="rounded-lg border p-3 space-y-3" data-testid={`order-item-${index}`}>
              <div className="flex items-center justify-between">
                <span className="text-sm font-medium">Item {index + 1}</span>
                <Button type="button" variant="ghost" size="sm" onClick={() => removeItem(index)} disabled={items.length === 1}>Remove</Button>
              </div>
              <div className="grid gap-3 md:grid-cols-2">
                <div className="space-y-1 md:col-span-2">
                  <label className="text-sm font-medium">Product Name *</label>
                  <input data-testid={`item-name-${index}`} className="h-9 w-full rounded-md border px-3 text-sm" value={item.productName} onChange={(e) => updateItem(index, { productName: e.target.value })} />
                </div>
                <div className="space-y-1">
                  <label className="text-sm font-medium">Quantity *</label>
                  <input data-testid={`item-quantity-${index}`} type="number" min={1} className="h-9 w-full rounded-md border px-3 text-sm" value={item.quantity} onChange={(e) => updateItem(index, { quantity: Number(e.target.value) })} />
                </div>
                <div className="space-y-1">
                  <label className="text-sm font-medium">Unit Price *</label>
                  <input data-testid={`item-unitprice-${index}`} type="number" min={0} step="0.01" className="h-9 w-full rounded-md border px-3 text-sm" value={item.unitPrice} onChange={(e) => updateItem(index, { unitPrice: Number(e.target.value) })} />
                </div>
                <div className="space-y-1">
                  <label className="text-sm font-medium">SKU</label>
                  <input className="h-9 w-full rounded-md border px-3 text-sm" value={item.sku || ''} onChange={(e) => updateItem(index, { sku: e.target.value })} />
                </div>
                <div className="space-y-1">
                  <label className="text-sm font-medium">Description</label>
                  <input className="h-9 w-full rounded-md border px-3 text-sm" value={item.description || ''} onChange={(e) => updateItem(index, { description: e.target.value })} />
                </div>
              </div>
            </div>
          ))}
        </div>
        <div className="text-sm text-muted-foreground">Estimated subtotal: ${itemTotal.toFixed(2)}</div>
      </section>

      <section className="rounded-xl border p-4 space-y-3">
        <div className="flex items-center gap-2">
          <input id="include-shipping" type="checkbox" checked={includeShipping} onChange={(e) => setIncludeShipping(e.target.checked)} />
          <label htmlFor="include-shipping" className="text-sm font-medium">Include shipping details</label>
        </div>

        <div className="grid gap-3 md:grid-cols-2">
          <div className="space-y-1 md:col-span-2">
            <label className="text-sm font-medium">Address</label>
            <input className="h-9 w-full rounded-md border px-3 text-sm" value={address} onChange={(e) => setAddress(e.target.value)} />
          </div>
          <div className="space-y-1">
            <label className="text-sm font-medium">City</label>
            <input className="h-9 w-full rounded-md border px-3 text-sm" value={city} onChange={(e) => setCity(e.target.value)} />
          </div>
          <div className="space-y-1">
            <label className="text-sm font-medium">State</label>
            <input className="h-9 w-full rounded-md border px-3 text-sm" value={stateRegion} onChange={(e) => setStateRegion(e.target.value)} />
          </div>
          <div className="space-y-1">
            <label className="text-sm font-medium">Postal Code</label>
            <input className="h-9 w-full rounded-md border px-3 text-sm" value={postalCode} onChange={(e) => setPostalCode(e.target.value)} />
          </div>
          <div className="space-y-1">
            <label className="text-sm font-medium">Country</label>
            <input className="h-9 w-full rounded-md border px-3 text-sm" value={country} onChange={(e) => setCountry(e.target.value)} />
          </div>
          {includeShipping ? (
            <div className="space-y-1 md:col-span-2">
              <label className="text-sm font-medium">Shipping Method</label>
              <input className="h-9 w-full rounded-md border px-3 text-sm" value={shippingMethod} onChange={(e) => setShippingMethod(e.target.value)} placeholder="Standard" />
            </div>
          ) : null}
        </div>
      </section>

      <section className="rounded-xl border p-4 space-y-2">
        <label className="text-sm font-medium">Special Instructions</label>
        <textarea className="min-h-24 w-full rounded-md border px-3 py-2 text-sm" value={specialInstructions} onChange={(e) => setSpecialInstructions(e.target.value)} />
      </section>

      <div className="flex flex-wrap justify-end gap-2">
        <Button type="button" variant="outline" onClick={() => onCancel?.()} disabled={submitting}>Cancel</Button>
        <Button type="button" data-testid="submit-new-order-button" onClick={handleSubmit} disabled={submitting}>
          {submitting ? 'Creating...' : 'Create Order'}
        </Button>
      </div>
    </div>
  );
}
