using Microsoft.EntityFrameworkCore;
using SnackRack.Data;

namespace SnackRack.Pages.Features.Orders;

public record OrderHistoryRow(
    Guid OrderId,
    DateTimeOffset OrderCreatedAt,
    OrderStatus OrderStatus,
    Guid ProductId,
    string ProductName,
    decimal Price,
    int Quantity,
    Guid? ReviewId,
    string? ReviewTitle,
    string? ReviewComment
);

public class OrderHistoryQuery(ApplicationDbContext db, ILogger<OrderHistoryQuery> logger)
{
    public async Task<IEnumerable<OrderHistoryRow>> ExecuteAsync(string? userId, CancellationToken cancellationToken = default)
    {
        var orderItems = db.Orders
            .Where(o => o.Customer.UserId == userId)
            .SelectMany(o => o.OrderItems, (o, item) => new { Order = o, Item = item });

        var userReviews = db.Reviews
            .Where(r => r.User.Id == userId);

        var rows = await orderItems
            .LeftJoin(
                userReviews,
                x => x.Item.ProductId,
                r => r.Product.Id,
                (x, r) => new OrderHistoryRow(
                    x.Order.Id,
                    x.Order.CreatedAt,
                    x.Order.Status,
                    x.Item.ProductId,
                    x.Item.ProductName,
                    x.Item.Price,
                    x.Item.Quantity,
                    r != null ? r.Id : null,
                    r != null ? r.Title : null,
                    r != null ? r.Comment : null
                )
            )
            .ToListAsync(cancellationToken);

        logger.LogInformation("Retrieved {Count} order history rows for user {UserId}", rows.Count, userId);
        return rows.OrderByDescending(r => r.OrderCreatedAt);
    }
}
