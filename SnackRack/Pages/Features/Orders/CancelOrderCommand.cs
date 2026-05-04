using Microsoft.EntityFrameworkCore;
using SnackRack.Data;

namespace SnackRack.Pages.Features.Orders;

public enum CancelOutcome { Succeeded, NotFound, InvalidStatus, Failed }

public record CancelOrderResult(CancelOutcome Outcome, Guid? OrderId = null, string? Error = null);

public class CancelOrderCommand(ApplicationDbContext db, ILogger<CancelOrderCommand> logger)
{
    public async Task<CancelOrderResult> ExecuteAsync(Guid? orderId)
    {
        var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
            return new CancelOrderResult(CancelOutcome.NotFound);

        if (order.Status != OrderStatus.Pending)
            return new CancelOrderResult(CancelOutcome.InvalidStatus);

        order.Status = OrderStatus.Cancelled;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to cancel order {OrderId}", orderId);
            return new CancelOrderResult(CancelOutcome.Failed, orderId, "An error occurred while cancelling your order. Please try again.");
        }

        return new CancelOrderResult(CancelOutcome.Succeeded, order.Id);
    }
}
