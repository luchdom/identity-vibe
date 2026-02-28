using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orders.Data.Entities;
using Orders.Data.Seed.Models;
using Orders.Models.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Orders.Data;

public sealed class DatabaseSeeder
{
    private const string SeedOrdersRelativePath = "Data/Seed/orders.sample.v1.json";
    private readonly OrdersDbContext _context;
    private readonly string _contentRootPath;
    private readonly ILogger<DatabaseSeeder> _logger;

    private static readonly JsonSerializerOptions SeedJsonOptions = CreateJsonOptions();

    public DatabaseSeeder(
        OrdersDbContext context,
        string? contentRootPath = null,
        ILogger<DatabaseSeeder>? logger = null)
    {
        _context = context;
        _contentRootPath = string.IsNullOrWhiteSpace(contentRootPath)
            ? Directory.GetCurrentDirectory()
            : contentRootPath;
        _logger = logger ?? NullLogger<DatabaseSeeder>.Instance;
    }

    public async Task SeedAsync()
    {
        await SeedOrdersFromJsonIfEmptyAsync();
    }

    private async Task SeedOrdersFromJsonIfEmptyAsync()
    {
        if (await _context.Orders.AnyAsync())
        {
            _logger.LogInformation("Orders already exist, skipping sample JSON import");
            return;
        }

        var dataset = await LoadSeedDatasetAsync();
        ValidateDataset(dataset);

        var createdCustomers = 0;
        var reusedCustomers = 0;
        var createdOrders = 0;
        var createdItems = 0;
        var createdHistoryRows = 0;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var customersByEmail = await _context.Customers
            .ToDictionaryAsync(c => c.Email, StringComparer.OrdinalIgnoreCase);

        foreach (var seedOrder in dataset.Orders)
        {
            var customer = GetOrCreateCustomer(seedOrder.Customer, customersByEmail, ref createdCustomers, ref reusedCustomers);

            var order = new Order
            {
                OrderNumber = seedOrder.OrderNumber.Trim(),
                Customer = customer,
                UserId = seedOrder.UserId.Trim(),
                Status = seedOrder.Status,
                Source = string.IsNullOrWhiteSpace(seedOrder.Source) ? "SEED_JSON" : seedOrder.Source.Trim(),
                Currency = string.IsNullOrWhiteSpace(seedOrder.Currency) ? "USD" : seedOrder.Currency.Trim().ToUpperInvariant(),
                PaymentMethod = NormalizeNullable(seedOrder.PaymentMethod),
                PaymentReference = NormalizeNullable(seedOrder.PaymentReference),
                TrackingNumber = NormalizeNullable(seedOrder.TrackingNumber),
                SpecialInstructions = NormalizeNullable(seedOrder.SpecialInstructions),
                InternalNotes = NormalizeNullable(seedOrder.InternalNotes),
                TaxAmount = RoundCurrency(seedOrder.TaxAmount),
                ShippingAmount = RoundCurrency(seedOrder.ShippingAmount),
                DiscountAmount = RoundCurrency(seedOrder.DiscountAmount),
                ShippingAddress = NormalizeNullable(seedOrder.Customer.Address),
                ShippingCity = NormalizeNullable(seedOrder.Customer.City),
                ShippingState = NormalizeNullable(seedOrder.Customer.State),
                ShippingPostalCode = NormalizeNullable(seedOrder.Customer.PostalCode),
                ShippingCountry = NormalizeNullable(seedOrder.Customer.Country),
                CreatedAt = seedOrder.CreatedAt,
                UpdatedAt = seedOrder.UpdatedAt
            };

            var items = seedOrder.Items.Select(item => new OrderItem
            {
                Order = order,
                ProductSku = item.ProductSku.Trim(),
                ProductName = item.ProductName.Trim(),
                ProductDescription = NormalizeNullable(item.ProductDescription),
                ProductImageUrl = NormalizeNullable(item.ProductImageUrl),
                ProductCategory = NormalizeNullable(item.ProductCategory),
                Quantity = item.Quantity,
                UnitPrice = RoundCurrency(item.UnitPrice),
                DiscountAmount = RoundCurrency(item.DiscountAmount),
                Size = NormalizeNullable(item.Size),
                Color = NormalizeNullable(item.Color),
                AdditionalAttributes = NormalizeNullable(item.AdditionalAttributes),
                CreatedAt = seedOrder.CreatedAt,
                UpdatedAt = seedOrder.UpdatedAt
            }).ToList();

            foreach (var item in items)
            {
                item.CalculateLineTotal();
                item.CreatedAt = seedOrder.CreatedAt;
                item.UpdatedAt = seedOrder.UpdatedAt;
            }

            order.OrderItems = items;
            order.UpdateTotals();
            order.CreatedAt = seedOrder.CreatedAt;
            order.UpdatedAt = seedOrder.UpdatedAt;

            ApplyStatusTimestamps(order, seedOrder);

            var historyRows = seedOrder.StatusHistory
                .OrderBy(h => h.CreatedAt)
                .Select(history => new OrderStatusHistory
                {
                    Order = order,
                    FromStatus = history.FromStatus,
                    ToStatus = history.ToStatus,
                    Reason = NormalizeNullable(history.Reason),
                    Notes = NormalizeNullable(history.Notes),
                    ChangedByUserId = history.ChangedByUserId.Trim(),
                    ChangedByUserName = NormalizeNullable(history.ChangedByUserName),
                    Source = "SEED_JSON",
                    CreatedAt = history.CreatedAt
                })
                .ToList();

            order.StatusHistory = historyRows;

            _context.Orders.Add(order);
            createdOrders++;
            createdItems += items.Count;
            createdHistoryRows += historyRows.Count;
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        _logger.LogInformation(
            "Imported sample orders JSON dataset v{Version}: {Orders} orders, {Items} items, {HistoryRows} history rows, {CreatedCustomers} customers created, {ReusedCustomers} customers reused",
            dataset.Version,
            createdOrders,
            createdItems,
            createdHistoryRows,
            createdCustomers,
            reusedCustomers);
    }

    private async Task<OrdersSeedDataset> LoadSeedDatasetAsync()
    {
        var path = Path.Combine(_contentRootPath, SeedOrdersRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Orders seed dataset file not found: {path}");
        }

        var json = await File.ReadAllTextAsync(path);
        var dataset = JsonSerializer.Deserialize<OrdersSeedDataset>(json, SeedJsonOptions);
        if (dataset is null)
        {
            throw new InvalidOperationException($"Failed to deserialize orders seed dataset from '{path}'.");
        }

        return dataset;
    }

    private static void ValidateDataset(OrdersSeedDataset dataset)
    {
        if (dataset.Version <= 0)
        {
            throw new InvalidOperationException("Orders seed dataset version must be greater than zero.");
        }

        if (dataset.Orders is null || dataset.Orders.Count < 100)
        {
            throw new InvalidOperationException("Orders seed dataset must contain at least 100 orders.");
        }

        var orderNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < dataset.Orders.Count; i++)
        {
            var order = dataset.Orders[i];
            var prefix = $"orders[{i}]";

            if (string.IsNullOrWhiteSpace(order.OrderNumber))
                throw new InvalidOperationException($"{prefix}.orderNumber is required.");

            if (!orderNumbers.Add(order.OrderNumber.Trim()))
                throw new InvalidOperationException($"Duplicate orderNumber '{order.OrderNumber}'.");

            if (string.IsNullOrWhiteSpace(order.UserId))
                throw new InvalidOperationException($"{prefix}.userId is required.");

            if (!Enum.IsDefined(typeof(OrderStatus), order.Status))
                throw new InvalidOperationException($"{prefix}.status is invalid.");

            if (order.Items is null || order.Items.Count == 0)
                throw new InvalidOperationException($"{prefix}.items must contain at least one item.");

            if (order.StatusHistory is null || order.StatusHistory.Count == 0)
                throw new InvalidOperationException($"{prefix}.statusHistory must contain at least one row.");

            if (order.Customer is null)
                throw new InvalidOperationException($"{prefix}.customer is required.");

            if (string.IsNullOrWhiteSpace(order.Customer.Email))
                throw new InvalidOperationException($"{prefix}.customer.email is required.");

            if (string.IsNullOrWhiteSpace(order.Customer.FirstName) || string.IsNullOrWhiteSpace(order.Customer.LastName))
                throw new InvalidOperationException($"{prefix}.customer first/last name is required.");

            if (order.TaxAmount < 0 || order.ShippingAmount < 0 || order.DiscountAmount < 0)
                throw new InvalidOperationException($"{prefix} financial amounts cannot be negative.");

            foreach (var item in order.Items)
            {
                if (string.IsNullOrWhiteSpace(item.ProductSku))
                    throw new InvalidOperationException($"{prefix}.items productSku is required.");
                if (string.IsNullOrWhiteSpace(item.ProductName))
                    throw new InvalidOperationException($"{prefix}.items productName is required.");
                if (item.Quantity <= 0)
                    throw new InvalidOperationException($"{prefix}.items quantity must be > 0.");
                if (item.UnitPrice < 0 || item.DiscountAmount < 0)
                    throw new InvalidOperationException($"{prefix}.items prices cannot be negative.");
            }

            foreach (var history in order.StatusHistory)
            {
                if (!Enum.IsDefined(typeof(OrderStatus), history.FromStatus) || !Enum.IsDefined(typeof(OrderStatus), history.ToStatus))
                    throw new InvalidOperationException($"{prefix}.statusHistory contains invalid status values.");
                if (string.IsNullOrWhiteSpace(history.ChangedByUserId))
                    throw new InvalidOperationException($"{prefix}.statusHistory.changedByUserId is required.");
            }
        }
    }

    private Customer GetOrCreateCustomer(
        SeedCustomer seedCustomer,
        Dictionary<string, Customer> customersByEmail,
        ref int createdCustomers,
        ref int reusedCustomers)
    {
        var email = seedCustomer.Email.Trim();
        if (customersByEmail.TryGetValue(email, out var existing))
        {
            reusedCustomers++;
            return existing;
        }

        var customer = new Customer
        {
            Email = email,
            FirstName = seedCustomer.FirstName.Trim(),
            LastName = seedCustomer.LastName.Trim(),
            Phone = NormalizeNullable(seedCustomer.Phone),
            Address = NormalizeNullable(seedCustomer.Address),
            City = NormalizeNullable(seedCustomer.City),
            State = NormalizeNullable(seedCustomer.State),
            PostalCode = NormalizeNullable(seedCustomer.PostalCode),
            Country = NormalizeNullable(seedCustomer.Country),
            IsActive = true
        };

        customersByEmail[email] = customer;
        createdCustomers++;
        return customer;
    }

    private static void ApplyStatusTimestamps(Order order, SeedOrder seedOrder)
    {
        order.ShippedAt = seedOrder.ShippedAt;
        order.DeliveredAt = seedOrder.DeliveredAt;
        order.CancelledAt = seedOrder.CancelledAt;

        if (order.Status == OrderStatus.Shipped && order.ShippedAt is null)
        {
            order.ShippedAt = order.UpdatedAt;
        }

        if (order.Status == OrderStatus.Delivered)
        {
            order.ShippedAt ??= order.UpdatedAt;
            order.DeliveredAt ??= order.UpdatedAt;
        }

        if (order.Status == OrderStatus.Cancelled && order.CancelledAt is null)
        {
            order.CancelledAt = order.UpdatedAt;
        }
    }

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static decimal RoundCurrency(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
