using Microsoft.EntityFrameworkCore;
using Orders.Data.Entities;
using Orders.Models.Enums;

namespace Orders.Data;

public class DatabaseSeeder
{
    private readonly OrdersDbContext _context;

    public DatabaseSeeder(OrdersDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        // Seed sample customers
        await SeedCustomersAsync();
        
        // Seed sample orders
        await SeedSampleOrdersAsync();
    }

    private async Task SeedCustomersAsync()
    {
        if (!_context.Customers.Any())
        {
            var customers = new[]
            {
                new Customer
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@example.com",
                    Phone = "+1-555-0123",
                    Address = "123 Main St",
                    City = "New York",
                    State = "NY",
                    PostalCode = "10001",
                    Country = "USA"
                },
                new Customer
                {
                    FirstName = "Jane",
                    LastName = "Smith",
                    Email = "jane.smith@example.com",
                    Phone = "+1-555-0124",
                    Address = "456 Oak Ave",
                    City = "Los Angeles",
                    State = "CA",
                    PostalCode = "90210",
                    Country = "USA"
                }
            };

            _context.Customers.AddRange(customers);
            await _context.SaveChangesAsync();
        }
    }

    private async Task SeedSampleOrdersAsync()
    {
        if (!_context.Orders.Any())
        {
            var customer = await _context.Customers.FirstAsync();
            
            var sampleOrder = new Order
            {
                OrderNumber = "ORD-20250830-1001",
                CustomerId = customer.Id,
                UserId = "f4cf1e1b-4c4b-44cc-ba98-b6c75da5fafc", // Admin user ID
                Status = OrderStatus.Confirmed,
                SubtotalAmount = 299.97m,
                TaxAmount = 24.00m,
                ShippingAmount = 9.99m,
                TotalAmount = 333.96m,
                Currency = "USD",
                SpecialInstructions = "Sample order for demonstration",
                Source = "SEED_DATA",
                ShippingAddress = customer.Address,
                ShippingCity = customer.City,
                ShippingState = customer.State,
                ShippingPostalCode = customer.PostalCode,
                ShippingCountry = customer.Country
            };

            _context.Orders.Add(sampleOrder);
            await _context.SaveChangesAsync();

            // Add sample order items
            var orderItems = new[]
            {
                new OrderItem
                {
                    OrderId = sampleOrder.Id,
                    ProductSku = "LAPTOP-001",
                    ProductName = "Gaming Laptop Pro",
                    ProductDescription = "High-performance gaming laptop with RTX graphics",
                    ProductCategory = "Electronics",
                    Quantity = 1,
                    UnitPrice = 199.99m,
                    LineTotal = 199.99m
                },
                new OrderItem
                {
                    OrderId = sampleOrder.Id,
                    ProductSku = "MOUSE-001",
                    ProductName = "Wireless Gaming Mouse",
                    ProductDescription = "Ergonomic wireless mouse with RGB lighting",
                    ProductCategory = "Accessories",
                    Quantity = 1,
                    UnitPrice = 49.99m,
                    LineTotal = 49.99m
                },
                new OrderItem
                {
                    OrderId = sampleOrder.Id,
                    ProductSku = "KEYB-001",
                    ProductName = "Mechanical Keyboard",
                    ProductDescription = "RGB backlit mechanical keyboard",
                    ProductCategory = "Accessories",
                    Quantity = 1,
                    UnitPrice = 49.99m,
                    LineTotal = 49.99m
                }
            };

            _context.OrderItems.AddRange(orderItems);

            // Add status history
            var statusHistory = new OrderStatusHistory
            {
                OrderId = sampleOrder.Id,
                FromStatus = OrderStatus.Draft,
                ToStatus = OrderStatus.Confirmed,
                Reason = "Sample order confirmation",
                ChangedByUserId = "SYSTEM",
                ChangedByUserName = "System"
            };

            _context.OrderStatusHistory.Add(statusHistory);
            await _context.SaveChangesAsync();

            // Update order totals
            sampleOrder.UpdateTotals();
            await _context.SaveChangesAsync();
        }
    }
}