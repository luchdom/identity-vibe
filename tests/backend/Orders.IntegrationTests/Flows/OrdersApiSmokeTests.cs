using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Orders.IntegrationTests.Infrastructure;
using Orders.Models.Enums;
using Orders.Models.Responses;
using Xunit;

namespace Orders.IntegrationTests.Flows;

public sealed class OrdersApiSmokeTests : IAsyncLifetime
{
    private readonly OrdersApiFactory factory = new();

    public async Task InitializeAsync()
    {
        _ = factory.CreateApiClient();
        await factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Create_order_then_get_by_id_returns_created_order()
    {
        using var client = factory.CreateApiClient("buyer-1", "orders.read", "orders.write");

        var createResponse = await client.PostAsJsonAsync("/orders", CreateOrderRequest("SKU-001", "buyer-1@example.test"));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
        createdOrder.Should().NotBeNull();
        createdOrder!.Customer.Email.Should().Be("buyer-1@example.test");
        createdOrder.Items.Should().HaveCount(1);
        createdOrder.Status.Should().Be(OrderStatus.Draft);

        var getResponse = await client.GetAsync($"/orders/{createdOrder.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loadedOrder = await getResponse.Content.ReadFromJsonAsync<OrderResponse>();
        loadedOrder.Should().NotBeNull();
        loadedOrder!.Id.Should().Be(createdOrder.Id);
        loadedOrder.OrderNumber.Should().Be(createdOrder.OrderNumber);
        loadedOrder.Customer.Email.Should().Be(createdOrder.Customer.Email);
    }

    [Fact]
    public async Task Get_orders_returns_only_orders_for_the_authenticated_user()
    {
        using var buyerOneClient = factory.CreateApiClient("buyer-1", "orders.read", "orders.write");
        using var buyerTwoClient = factory.CreateApiClient("buyer-2", "orders.read", "orders.write");

        await buyerOneClient.PostAsJsonAsync("/orders", CreateOrderRequest("SKU-101", "buyer-1@example.test"));
        await buyerOneClient.PostAsJsonAsync("/orders", CreateOrderRequest("SKU-102", "buyer-1@example.test"));
        await buyerTwoClient.PostAsJsonAsync("/orders", CreateOrderRequest("SKU-201", "buyer-2@example.test"));

        var response = await buyerOneClient.GetAsync("/orders?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.Content.ReadFromJsonAsync<OrderListResponse>();
        orders.Should().NotBeNull();
        orders!.Orders.Should().HaveCount(2);
        orders.TotalCount.Should().Be(2);
        orders.PageNumber.Should().Be(1);
        orders.PageSize.Should().Be(10);
        orders.Orders.Select(order => order.CustomerEmail).Should().OnlyContain(email => email == "buyer-1@example.test");
    }

    [Fact]
    public async Task Update_order_status_with_manage_scope_updates_status_endpoint()
    {
        using var writeClient = factory.CreateApiClient("manager-1", "orders.read", "orders.write");
        using var manageClient = factory.CreateApiClient("manager-1", "orders.read", "orders.manage");

        var createResponse = await writeClient.PostAsJsonAsync("/orders", CreateOrderRequest("SKU-301", "manager-1@example.test"));
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();

        var updateResponse = await manageClient.PostAsJsonAsync(
            $"/orders/{createdOrder!.Id}/status",
            new
            {
                Status = OrderStatus.Confirmed,
                Reason = "Ready for processing"
            });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedOrder = await updateResponse.Content.ReadFromJsonAsync<OrderResponse>();
        updatedOrder.Should().NotBeNull();
        updatedOrder!.Status.Should().Be(OrderStatus.Confirmed);

        var statusResponse = await manageClient.GetAsync($"/orders/{createdOrder.Id}/status");

        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var orderStatus = await statusResponse.Content.ReadFromJsonAsync<OrderStatusResponse>();
        orderStatus.Should().NotBeNull();
        orderStatus!.CurrentStatus.Should().Be(OrderStatus.Confirmed);
        orderStatus.StatusHistory.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_order_with_invalid_payload_returns_validation_problem_details()
    {
        using var client = factory.CreateApiClient("buyer-1", "orders.read", "orders.write");

        var response = await client.PostAsJsonAsync(
            "/orders",
            new
            {
                Customer = new
                {
                    FirstName = "",
                    LastName = "Buyer",
                    Email = "not-an-email"
                },
                Items = Array.Empty<object>()
            });

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, because: body);

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        root.GetProperty("title").GetString().Should().Be("Validation Error");
        root.GetProperty("status").GetInt32().Should().Be((int)HttpStatusCode.BadRequest);
        root.GetProperty("instance").GetString().Should().Be("/orders");
        root.GetProperty("errors").EnumerateObject().Select(property => property.Name)
            .Should().Contain(name => string.Equals(name, "Customer.FirstName", StringComparison.OrdinalIgnoreCase));
    }

    private static object CreateOrderRequest(string sku, string customerEmail)
    {
        return new
        {
            Customer = new
            {
                FirstName = "Test",
                LastName = "Buyer",
                Email = customerEmail,
                Phone = "555-0100",
                Address = "123 Test Street",
                City = "Testville",
                State = "TS",
                PostalCode = "12345",
                Country = "BR"
            },
            Items = new[]
            {
                new
                {
                    ProductSku = sku,
                    ProductName = $"Product {sku}",
                    Quantity = 1,
                    UnitPrice = 25.50m
                }
            },
            Shipping = new
            {
                Address = "123 Test Street",
                City = "Testville",
                State = "TS",
                PostalCode = "12345",
                Country = "BR",
                ShippingCost = 5.00m
            },
            SpecialInstructions = "Leave at reception"
        };
    }
}
