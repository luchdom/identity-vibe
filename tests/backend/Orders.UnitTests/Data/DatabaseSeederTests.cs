using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Orders.Data;
using Orders.Data.Entities;
using Orders.Data.Seed.Models;
using Orders.Models.Enums;
using Xunit;

namespace Orders.UnitTests.Data;

public sealed class DatabaseSeederTests
{
    [Fact]
    public async Task SeedAsync_ShouldImportCommittedJsonDataset_WhenOrdersTableIsEmpty()
    {
        await using var fixture = await CreateContextFixtureAsync();
        var seeder = new DatabaseSeeder(fixture.Context, GetOrdersProjectRoot());

        await seeder.SeedAsync();

        var orders = await fixture.Context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.StatusHistory)
            .ToListAsync();
        var customers = await fixture.Context.Customers.ToListAsync();

        orders.Should().HaveCount(150);
        customers.Should().NotBeEmpty();
        orders.Select(o => o.Status).Distinct().Should().Contain([
            OrderStatus.Draft,
            OrderStatus.Confirmed,
            OrderStatus.Processing,
            OrderStatus.Shipped,
            OrderStatus.Delivered,
            OrderStatus.Cancelled
        ]);
        orders.Should().OnlyContain(o => o.Source == "SEED_JSON");
        orders.Should().OnlyContain(o => o.UserId.StartsWith("seed-user-", StringComparison.Ordinal));
        orders.Should().OnlyContain(o => o.OrderItems.Count > 0);
        orders.Should().OnlyContain(o => o.StatusHistory.Count > 0);

        foreach (var order in orders.Take(10))
        {
            var expectedSubtotal = order.OrderItems.Sum(i => i.LineTotal);
            var expectedTotal = expectedSubtotal + order.TaxAmount + order.ShippingAmount - order.DiscountAmount;
            order.SubtotalAmount.Should().Be(expectedSubtotal);
            order.TotalAmount.Should().Be(expectedTotal);
        }
    }

    [Fact]
    public async Task SeedAsync_ShouldSkip_WhenOrdersAlreadyExist()
    {
        await using var fixture = await CreateContextFixtureAsync();

        var customer = new Customer
        {
            FirstName = "Skip",
            LastName = "Test",
            Email = "skip@example.com"
        };
        fixture.Context.Customers.Add(customer);
        fixture.Context.Orders.Add(new Order
        {
            OrderNumber = "EXISTING-0001",
            Customer = customer,
            UserId = "seed-user-existing",
            Status = OrderStatus.Draft,
            Source = "TEST",
            Currency = "USD"
        });
        await fixture.Context.SaveChangesAsync();

        var seeder = new DatabaseSeeder(fixture.Context, GetOrdersProjectRoot());
        await seeder.SeedAsync();

        (await fixture.Context.Orders.CountAsync()).Should().Be(1);
        (await fixture.Context.Customers.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SeedAsync_ShouldReuseExistingCustomerByEmail()
    {
        var dataset = await LoadCommittedDatasetAsync();
        var firstSeedOrder = dataset.Orders.First();

        await using var fixture = await CreateContextFixtureAsync();

        var existingCustomer = new Customer
        {
            FirstName = "Existing",
            LastName = "Customer",
            Email = firstSeedOrder.Customer.Email,
            Phone = "555-0000"
        };
        fixture.Context.Customers.Add(existingCustomer);
        await fixture.Context.SaveChangesAsync();
        var existingCustomerId = existingCustomer.Id;

        var seeder = new DatabaseSeeder(fixture.Context, GetOrdersProjectRoot());
        await seeder.SeedAsync();

        var seededOrder = await fixture.Context.Orders
            .AsNoTracking()
            .SingleAsync(o => o.OrderNumber == firstSeedOrder.OrderNumber);
        var customerAfter = await fixture.Context.Customers
            .AsNoTracking()
            .SingleAsync(c => c.Id == existingCustomerId);

        seededOrder.CustomerId.Should().Be(existingCustomerId);
        customerAfter.FirstName.Should().Be("Existing");
        customerAfter.LastName.Should().Be("Customer");
        customerAfter.Phone.Should().Be("555-0000");
        var matchingEmailCustomerCount = await fixture.Context.Customers.CountAsync(c => c.Email == firstSeedOrder.Customer.Email);
        matchingEmailCustomerCount.Should().Be(1);
    }

    [Fact]
    public async Task SeedAsync_ShouldFailFast_WhenDatasetContainsDuplicateOrderNumbers()
    {
        var dataset = await LoadCommittedDatasetAsync();
        var mutatedOrders = dataset.Orders.ToList();
        mutatedOrders[1] = mutatedOrders[1] with { OrderNumber = mutatedOrders[0].OrderNumber };
        var invalidDataset = dataset with { Orders = mutatedOrders };

        using var tempRoot = new TempDirectory();
        WriteDataset(tempRoot.Path, invalidDataset);

        await using var fixture = await CreateContextFixtureAsync();
        var seeder = new DatabaseSeeder(fixture.Context, tempRoot.Path);

        var act = async () => await seeder.SeedAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Duplicate orderNumber*");
    }

    private static async Task<OrdersSeedDataset> LoadCommittedDatasetAsync()
    {
        var ordersRoot = GetOrdersProjectRoot();
        var path = Path.Combine(ordersRoot, "Data", "Seed", "orders.sample.v1.json");
        var json = await File.ReadAllTextAsync(path);
        var dataset = JsonSerializer.Deserialize<OrdersSeedDataset>(json, CreateJsonOptions());
        return dataset ?? throw new InvalidOperationException("Failed to load committed orders seed dataset.");
    }

    private static void WriteDataset(string contentRootPath, OrdersSeedDataset dataset)
    {
        var seedDir = Path.Combine(contentRootPath, "Data", "Seed");
        Directory.CreateDirectory(seedDir);
        var path = Path.Combine(seedDir, "orders.sample.v1.json");
        var json = JsonSerializer.Serialize(dataset, CreateJsonOptions());
        File.WriteAllText(path, json);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static string GetOrdersProjectRoot([CallerFilePath] string callerFilePath = "")
    {
        var dir = new DirectoryInfo(Path.GetDirectoryName(callerFilePath)!);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ArchZ.sln")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new InvalidOperationException("Could not locate repository root from test source path.");
        }

        var repoRoot = dir.FullName;
        var ordersRoot = Path.Combine(repoRoot, "src", "backend", "Orders");
        if (!File.Exists(Path.Combine(ordersRoot, "Orders.csproj")))
        {
            throw new InvalidOperationException("Could not locate Orders project root from repository root.");
        }

        return ordersRoot;
    }

    private static async Task<DbFixture> CreateContextFixtureAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new OrdersDbContext(options);
        await context.Database.EnsureCreatedAsync();

        return new DbFixture(context, connection);
    }

    private sealed class DbFixture : IAsyncDisposable
    {
        public DbFixture(OrdersDbContext context, SqliteConnection connection)
        {
            Context = context;
            Connection = connection;
        }

        public OrdersDbContext Context { get; }
        public SqliteConnection Connection { get; }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "identity-vibe-orders-seed-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
