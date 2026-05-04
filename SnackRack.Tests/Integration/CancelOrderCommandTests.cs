using SnackRack.Pages.Features.Customers;
using SnackRack.Pages.Features.Orders;
using Microsoft.Extensions.Logging.Abstractions;

namespace SnackRack.Tests.Integration;

/// <summary>
/// Integration tests for CancelOrderCommand.
/// Covers spec criteria #21–24 from Pages/Features/Orders/SPEC.md.
/// </summary>
[Collection("Database")]
public class CancelOrderCommandTests(TestDatabaseFixture fixture)
{
    private CancelOrderCommand BuildCommand() =>
        new(_fixture.DbContext, NullLogger<CancelOrderCommand>.Instance);

    private readonly TestDatabaseFixture _fixture = fixture;

    /// <summary>
    /// SCENARIO: Cancelling a pending order.
    /// GIVEN:    An order exists with status Pending.
    /// WHEN:     CancelOrderCommand is executed with its ID.
    /// THEN:     The order status is set to Cancelled and the result is Succeeded.
    /// SPEC:     #21 — Cancelling an Order
    /// </summary>
    [Fact]
    public async Task Cancel_PendingOrder_SetsStatusCancelledAndSucceeds()
    {
        var order = new Order
        {
            Id = Guid.CreateVersion7(),
            Customer = new Customer(),
            Status = OrderStatus.Pending,
            OrderItems = []
        };
        _fixture.DbContext.Orders.Add(order);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var result = await BuildCommand().ExecuteAsync(order.Id);

        Assert.Equal(CancelOutcome.Succeeded, result.Outcome);
        Assert.Equal(order.Id, result.OrderId);

        var saved = await _fixture.DbContext.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Cancelled, saved!.Status);
    }

    /// <summary>
    /// SCENARIO: Cancelling an order that does not exist.
    /// GIVEN:    No order exists for the given ID.
    /// WHEN:     CancelOrderCommand is executed.
    /// THEN:     The result outcome is NotFound.
    /// SPEC:     #22 — Cancelling an Order
    /// </summary>
    [Fact]
    public async Task Cancel_UnknownOrderId_ReturnsNotFound()
    {
        var result = await BuildCommand().ExecuteAsync(Guid.NewGuid());

        Assert.Equal(CancelOutcome.NotFound, result.Outcome);
    }

    /// <summary>
    /// SCENARIO: Cancelling an order that is not Pending.
    /// GIVEN:    An order exists with status Submitted.
    /// WHEN:     CancelOrderCommand is executed with its ID.
    /// THEN:     The result outcome is InvalidStatus and the order is unchanged.
    /// SPEC:     #23 — Cancelling an Order
    /// </summary>
    [Theory]
    [InlineData(OrderStatus.Submitted)]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task Cancel_NonPendingOrder_ReturnsInvalidStatus(OrderStatus status)
    {
        var order = new Order
        {
            Id = Guid.CreateVersion7(),
            Customer = new Customer(),
            Status = status,
            OrderItems = []
        };
        _fixture.DbContext.Orders.Add(order);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var result = await BuildCommand().ExecuteAsync(order.Id);

        Assert.Equal(CancelOutcome.InvalidStatus, result.Outcome);

        var saved = await _fixture.DbContext.Orders.FindAsync(order.Id);
        Assert.Equal(status, saved!.Status);
    }
}
