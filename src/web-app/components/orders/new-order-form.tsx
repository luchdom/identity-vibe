'use client';

import { useMemo, useState } from 'react';

import { zodResolver } from '@hookform/resolvers/zod';
import { Icon } from '@iconify/react';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Divider from '@mui/material/Divider';
import FormControlLabel from '@mui/material/FormControlLabel';
import IconButton from '@mui/material/IconButton';
import Stack from '@mui/material/Stack';
import Switch from '@mui/material/Switch';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import { useFieldArray, useForm, useWatch } from 'react-hook-form';
import { z } from 'zod';

import { SurfacePanel } from '@/components/common/surface-panel';
import { ordersApi } from '@/lib/orders-api';
import type { CreateOrderRequest } from '@/types/orders';

const orderItemSchema = z.object({
  productSku: z.string().trim().min(1, 'SKU is required'),
  productName: z.string().trim().min(1, 'Product name is required'),
  productDescription: z.string().trim().optional(),
  quantity: z.number().int().min(1, 'Quantity must be at least 1'),
  unitPrice: z.number().min(0.01, 'Unit price must be greater than 0'),
});

const newOrderSchema = z.object({
  customerFirstName: z.string().trim().min(1, 'First name is required'),
  customerLastName: z.string().trim().min(1, 'Last name is required'),
  customerEmail: z.string().trim().email('Enter a valid email'),
  customerPhone: z.string().trim().optional(),
  address: z.string().trim().optional(),
  city: z.string().trim().optional(),
  state: z.string().trim().optional(),
  postalCode: z.string().trim().optional(),
  country: z.string().trim().optional(),
  includeShipping: z.boolean(),
  shippingCost: z.number().min(0, 'Shipping cost must be >= 0').optional(),
  specialInstructions: z.string().trim().optional(),
  items: z.array(orderItemSchema).min(1, 'At least one item is required'),
});

type NewOrderFormValues = z.infer<typeof newOrderSchema>;

type NewOrderFormProps = {
  onCancel?: () => void;
  onCreated?: (orderId: number) => void;
};

const defaultValues: NewOrderFormValues = {
  customerFirstName: '',
  customerLastName: '',
  customerEmail: '',
  customerPhone: '',
  address: '',
  city: '',
  state: '',
  postalCode: '',
  country: '',
  includeShipping: false,
  shippingCost: 0,
  specialInstructions: '',
  items: [
    {
      productSku: '',
      productName: '',
      productDescription: '',
      quantity: 1,
      unitPrice: 1,
    },
  ],
};

export function NewOrderForm({ onCancel, onCreated }: NewOrderFormProps) {
  const [submitError, setSubmitError] = useState<string | null>(null);

  const {
    register,
    control,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<NewOrderFormValues>({
    resolver: zodResolver(newOrderSchema),
    defaultValues,
    mode: 'onChange',
  });

  const { fields, append, remove } = useFieldArray({
    control,
    name: 'items',
  });

  const items = useWatch({ control, name: 'items' });
  const includeShipping = useWatch({ control, name: 'includeShipping' });

  const subtotal = useMemo(
    () => (items || []).reduce((sum, item) => sum + (Number(item.quantity) || 0) * (Number(item.unitPrice) || 0), 0),
    [items]
  );

  const onSubmit = handleSubmit(async (values) => {
    setSubmitError(null);

    const payload: CreateOrderRequest = {
      customer: {
        firstName: values.customerFirstName.trim(),
        lastName: values.customerLastName.trim(),
        email: values.customerEmail.trim(),
        phone: values.customerPhone?.trim() || undefined,
        address: values.address?.trim() || undefined,
        city: values.city?.trim() || undefined,
        state: values.state?.trim() || undefined,
        postalCode: values.postalCode?.trim() || undefined,
        country: values.country?.trim() || undefined,
      },
      items: values.items.map((item) => ({
        productSku: item.productSku.trim(),
        productName: item.productName.trim(),
        productDescription: item.productDescription?.trim() || undefined,
        quantity: Number(item.quantity),
        unitPrice: Number(item.unitPrice),
      })),
      shipping: values.includeShipping
        ? {
            address: values.address?.trim() || undefined,
            city: values.city?.trim() || undefined,
            state: values.state?.trim() || undefined,
            postalCode: values.postalCode?.trim() || undefined,
            country: values.country?.trim() || undefined,
            shippingCost: Number(values.shippingCost) || 0,
          }
        : undefined,
      specialInstructions: values.specialInstructions?.trim() || undefined,
    };

    try {
      const response = await ordersApi.createOrder(payload);
      onCreated?.(response.order.id);
    } catch (error: unknown) {
      const maybeAxios = error as { response?: { data?: { detail?: string; message?: string } }; message?: string };
      setSubmitError(maybeAxios.response?.data?.detail || maybeAxios.response?.data?.message || maybeAxios.message || 'Failed to create order');
      throw error;
    }
  });

  return (
    <Stack spacing={2.5} data-testid="new-order-form">
      <SurfacePanel>
        <Stack spacing={2} sx={{ p: 2.5 }}>
          <Typography variant="subtitle1" fontWeight={700}>Customer</Typography>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
            <TextField
              {...register('customerFirstName')}
              label="First name"
              size="small"
              fullWidth
              error={Boolean(errors.customerFirstName)}
              helperText={errors.customerFirstName?.message}
              inputProps={{ 'data-testid': 'customer-first-name' }}
            />
            <TextField
              {...register('customerLastName')}
              label="Last name"
              size="small"
              fullWidth
              error={Boolean(errors.customerLastName)}
              helperText={errors.customerLastName?.message}
              inputProps={{ 'data-testid': 'customer-last-name' }}
            />
          </Stack>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
            <TextField
              {...register('customerEmail')}
              label="Email"
              size="small"
              fullWidth
              error={Boolean(errors.customerEmail)}
              helperText={errors.customerEmail?.message}
              inputProps={{ 'data-testid': 'customer-email' }}
            />
            <TextField
              {...register('customerPhone')}
              label="Phone"
              size="small"
              fullWidth
            />
          </Stack>
        </Stack>
      </SurfacePanel>

      <SurfacePanel>
        <Stack spacing={2} sx={{ p: 2.5 }}>
          <Stack direction="row" justifyContent="space-between" alignItems="center">
            <Typography variant="subtitle1" fontWeight={700}>Items</Typography>
            <Button
              size="small"
              variant="outlined"
              startIcon={<Icon icon="mingcute:add-line" width={16} />}
              onClick={() => append({
                productSku: '',
                productName: '',
                productDescription: '',
                quantity: 1,
                unitPrice: 1,
              })}
              sx={{ height: 36, borderRadius: 1, px: 1.5 }}
            >
              Add item
            </Button>
          </Stack>

          {errors.items?.message ? (
            <Alert severity="error" variant="outlined">{errors.items.message}</Alert>
          ) : null}

          <Stack spacing={1.5}>
            {fields.map((field, index) => (
              <Box
                key={field.id}
                sx={{
                  border: '1px solid',
                  borderColor: 'divider',
                  borderRadius: 1,
                  p: 1.5,
                }}
              >
                <Stack spacing={1.5}>
                  <Stack direction="row" justifyContent="space-between" alignItems="center">
                    <Typography variant="body2" fontWeight={700}>Item {index + 1}</Typography>
                    <IconButton
                      size="small"
                      onClick={() => remove(index)}
                      disabled={fields.length === 1}
                      aria-label="Remove item"
                    >
                      <Icon icon="solar:trash-bin-trash-bold" width={16} />
                    </IconButton>
                  </Stack>

                  <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5}>
                    <TextField
                      {...register(`items.${index}.productSku`)}
                      label="SKU"
                      size="small"
                      fullWidth
                      error={Boolean(errors.items?.[index]?.productSku)}
                      helperText={errors.items?.[index]?.productSku?.message}
                    />
                    <TextField
                      {...register(`items.${index}.productName`)}
                      label="Product name"
                      size="small"
                      fullWidth
                      error={Boolean(errors.items?.[index]?.productName)}
                      helperText={errors.items?.[index]?.productName?.message}
                    />
                  </Stack>

                  <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5}>
                    <TextField
                      {...register(`items.${index}.quantity`, { valueAsNumber: true })}
                      type="number"
                      label="Quantity"
                      size="small"
                      fullWidth
                      error={Boolean(errors.items?.[index]?.quantity)}
                      helperText={errors.items?.[index]?.quantity?.message}
                    />
                    <TextField
                      {...register(`items.${index}.unitPrice`, { valueAsNumber: true })}
                      type="number"
                      label="Unit price"
                      size="small"
                      fullWidth
                      error={Boolean(errors.items?.[index]?.unitPrice)}
                      helperText={errors.items?.[index]?.unitPrice?.message}
                    />
                  </Stack>

                  <TextField
                    {...register(`items.${index}.productDescription`)}
                    label="Description"
                    size="small"
                    fullWidth
                  />
                </Stack>
              </Box>
            ))}
          </Stack>

          <Divider />
          <Typography variant="body2" color="text.secondary">
            Estimated subtotal: <strong>${subtotal.toFixed(2)}</strong>
          </Typography>
        </Stack>
      </SurfacePanel>

      <SurfacePanel>
        <Stack spacing={2} sx={{ p: 2.5 }}>
          <Typography variant="subtitle1" fontWeight={700}>Shipping and notes</Typography>

          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
            <TextField {...register('address')} label="Address" size="small" fullWidth />
            <TextField {...register('city')} label="City" size="small" fullWidth />
          </Stack>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
            <TextField {...register('state')} label="State" size="small" fullWidth />
            <TextField {...register('postalCode')} label="Postal code" size="small" fullWidth />
            <TextField {...register('country')} label="Country" size="small" fullWidth />
          </Stack>

          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} alignItems={{ md: 'center' }}>
            <FormControlLabel
              control={<Switch size="small" {...register('includeShipping')} />}
              label="Include shipping"
            />
            {includeShipping ? (
              <TextField
                {...register('shippingCost', { valueAsNumber: true })}
                type="number"
                label="Shipping cost"
                size="small"
                error={Boolean(errors.shippingCost)}
                helperText={errors.shippingCost?.message}
                sx={{ minWidth: 180 }}
              />
            ) : null}
          </Stack>

          <TextField
            {...register('specialInstructions')}
            label="Special instructions"
            size="small"
            multiline
            minRows={3}
            fullWidth
          />
        </Stack>
      </SurfacePanel>

      {submitError ? (
        <Alert severity="error" variant="outlined">
          {submitError}
        </Alert>
      ) : null}

      <Stack direction="row" spacing={1.25} justifyContent="flex-end">
        <Button
          size="small"
          variant="outlined"
          onClick={onCancel}
          disabled={isSubmitting}
          sx={{ height: 36, borderRadius: 1, px: 1.5 }}
        >
          Cancel
        </Button>
        <Button
          size="small"
          variant="contained"
          color="inherit"
          onClick={() => void onSubmit()}
          disabled={isSubmitting}
          data-testid="submit-new-order-button"
          sx={{ height: 36, borderRadius: 1, px: 1.5 }}
        >
          {isSubmitting ? 'Creating...' : 'Create order'}
        </Button>
      </Stack>
    </Stack>
  );
}
