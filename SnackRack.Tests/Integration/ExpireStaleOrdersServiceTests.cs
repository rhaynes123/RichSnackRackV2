using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SnackRack.Data;
using SnackRack.Pages.Features.Customers;
using SnackRack.Pages.Features.Orders;

namespace SnackRack.Tests.Integration;

/// <summary>
/// Integration tests for ExpireStaleOrdersService.
/// Verifies that stale Pending orders (>24h) are auto-cancelled and that
/// recent or non-Pending orders are left unchanged.
/// </summary>
[Collection("Database")]
public class ExpireStaleOrdersServiceTests(TestDatabaseFixture fixture)
{
    private readonly TestDatabaseFixture _fixture = fixture;

    // IServiceScopeFactory is only used by ExecuteAsync (the timer loop).
    // Tests call CancelStaleOrdersAsync directly, so a stub scope factory suffices.
    private static ExpireStaleOrdersService BuildService() =>
        new(new Mock<IServiceScopeFactory>().Object,
            NullLogger<ExpireStaleOrdersService>.Instance);

    private static Order MakeOrder(OrderStatus status, DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Customer = new Customer(),
            Status = status,
            CreatedAt = createdAt,
            OrderItems = []
        };

    /// <summary>
    /// SCENARIO: Stale pending order is auto-cancelled.
    /// GIVEN:    A Pending order with CreatedAt 25 hours ago.
    /// WHEN:     CancelStaleOrdersAsync is called.
    /// THEN:     The order status is set to Cancelled.
    /// </summary>
    [Fact]
    public async Task ExpireStaleOrders_OldPendingOrders_GetCancelledAsync()
    {
        var order = MakeOrder(OrderStatus.Pending, DateTimeOffset.UtcNow.AddHours(-25));
        _fixture.DbContext.Orders.Add(order);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var service = BuildService();
        await service.CancelStaleOrdersAsync(_fixture.DbContext, CancellationToken.None);

        var saved = await _fixture.DbContext.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Cancelled, saved!.Status);
    }

    /// <summary>
    /// SCENARIO: Recent pending order is not cancelled.
    /// GIVEN:    A Pending order with CreatedAt 1 hour ago.
    /// WHEN:     CancelStaleOrdersAsync is called.
    /// THEN:     The order remains Pending.
    /// </summary>
    [Fact]
    public async Task ExpireStaleOrders_RecentPendingOrders_AreNotCancelled()
    {
        var order = MakeOrder(OrderStatus.Pending, DateTimeOffset.UtcNow.AddHours(-1));
        _fixture.DbContext.Orders.Add(order);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var service = BuildService();
        await service.CancelStaleOrdersAsync(_fixture.DbContext, CancellationToken.None);

        var saved = await _fixture.DbContext.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Pending, saved!.Status);
    }

    /// <summary>
    /// SCENARIO: Non-pending orders older than 24h are not touched.
    /// GIVEN:    Submitted and Completed orders with CreatedAt 48 hours ago.
    /// WHEN:     CancelStaleOrdersAsync is called.
    /// THEN:     Their statuses are unchanged.
    /// </summary>
    [Theory]
    [InlineData(OrderStatus.Submitted)]
    [InlineData(OrderStatus.Completed)]
    public async Task ExpireStaleOrders_NonPendingOldOrders_AreNotCancelled(OrderStatus status)
    {
        var order = MakeOrder(status, DateTimeOffset.UtcNow.AddHours(-48));
        _fixture.DbContext.Orders.Add(order);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var service = BuildService();
        await service.CancelStaleOrdersAsync(_fixture.DbContext, CancellationToken.None);

        var saved = await _fixture.DbContext.Orders.FindAsync(order.Id);
        Assert.Equal(status, saved!.Status);
    }
}
