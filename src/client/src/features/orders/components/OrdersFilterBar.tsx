import React, { useState, useCallback } from 'react';
import { Search, Filter, X } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { 
  Select, 
  SelectContent, 
  SelectItem, 
  SelectTrigger, 
  SelectValue 
} from '@/components/ui/select';
import { 
  Card, 
  CardContent 
} from '@/components/ui/card';
import { OrderStatus, OrderStatusLabels, type FilterState } from '@/types/orders';
import { useDebounce } from '@/hooks/use-debounce';


interface OrdersFilterBarProps {
  filters: FilterState;
  onFiltersChange: (filters: FilterState) => void;
  isLoading?: boolean;
  totalCount?: number;
}

export function OrdersFilterBar({ 
  filters, 
  onFiltersChange, 
  isLoading, 
  totalCount 
}: OrdersFilterBarProps) {
  const [searchInput, setSearchInput] = useState(filters.search);
  
  // Debounce search input to avoid excessive API calls
  const debouncedSearch = useDebounce(searchInput, 500);

  // Update filters when debounced search changes
  React.useEffect(() => {
    if (debouncedSearch !== filters.search) {
      onFiltersChange({ ...filters, search: debouncedSearch });
    }
  }, [debouncedSearch, filters, onFiltersChange]);

  const handleSearchChange = useCallback((value: string) => {
    setSearchInput(value);
  }, []);

  const handleStatusChange = useCallback((value: string) => {
    const status = value === 'all' ? undefined : parseInt(value) as OrderStatus;
    onFiltersChange({ ...filters, status });
  }, [filters, onFiltersChange]);

  const handleSortChange = useCallback((value: string) => {
    const [sortBy, sortOrder] = value.split(':');
    onFiltersChange({ 
      ...filters, 
      sortBy, 
      sortOrder: sortOrder as 'asc' | 'desc' 
    });
  }, [filters, onFiltersChange]);

  const handleClearFilters = useCallback(() => {
    setSearchInput('');
    onFiltersChange({
      search: '',
      status: undefined,
      sortBy: 'createdAt',
      sortOrder: 'desc',
    });
  }, [onFiltersChange]);

  const hasActiveFilters = filters.search || filters.status !== undefined;
  const sortValue = `${filters.sortBy}:${filters.sortOrder}`;

  return (
    <Card>
      <CardContent className="p-4">
        <div className="flex flex-col space-y-4 sm:flex-row sm:space-y-0 sm:space-x-4 sm:items-center">
          {/* Search Input */}
          <div className="relative flex-1 min-w-0">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              placeholder="Search orders, customers, or order numbers..."
              value={searchInput}
              onChange={(e) => handleSearchChange(e.target.value)}
              className="pl-9"
              disabled={isLoading}
            />
          </div>

          <div className="flex flex-col space-y-2 sm:flex-row sm:space-y-0 sm:space-x-2">
            {/* Status Filter */}
            <Select
              value={filters.status?.toString() ?? 'all'}
              onValueChange={handleStatusChange}
              disabled={isLoading}
            >
              <SelectTrigger className="w-full sm:w-[180px]">
                <Filter className="h-4 w-4 mr-2" />
                <SelectValue placeholder="Filter by status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Status</SelectItem>
                {Object.entries(OrderStatusLabels).map(([value, label]) => (
                  <SelectItem key={value} value={value}>
                    {label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            {/* Sort Options */}
            <Select
              value={sortValue}
              onValueChange={handleSortChange}
              disabled={isLoading}
            >
              <SelectTrigger className="w-full sm:w-[180px]">
                <SelectValue placeholder="Sort by" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="createdAt:desc">Newest First</SelectItem>
                <SelectItem value="createdAt:asc">Oldest First</SelectItem>
                <SelectItem value="orderNumber:asc">Order Number A-Z</SelectItem>
                <SelectItem value="orderNumber:desc">Order Number Z-A</SelectItem>
                <SelectItem value="totalAmount:desc">Highest Amount</SelectItem>
                <SelectItem value="totalAmount:asc">Lowest Amount</SelectItem>
                <SelectItem value="customerName:asc">Customer A-Z</SelectItem>
                <SelectItem value="customerName:desc">Customer Z-A</SelectItem>
              </SelectContent>
            </Select>

            {/* Clear Filters Button */}
            {hasActiveFilters && (
              <Button
                variant="outline"
                size="sm"
                onClick={handleClearFilters}
                disabled={isLoading}
                className="whitespace-nowrap"
              >
                <X className="h-4 w-4 mr-1" />
                Clear
              </Button>
            )}
          </div>
        </div>

        {/* Results Summary */}
        {totalCount !== undefined && (
          <div className="mt-3 text-sm text-muted-foreground">
            {isLoading ? (
              <span>Loading orders...</span>
            ) : (
              <span>
                Showing {totalCount.toLocaleString()} order{totalCount !== 1 ? 's' : ''}
                {hasActiveFilters && ' matching your filters'}
              </span>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
}