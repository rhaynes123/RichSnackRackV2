using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SnackRack.Pages.Features.Orders;

public class HistoryModel : PageModel
{
    public List<OrderGroup> OrderGroups { get; set; } = [];

    private readonly OrderHistoryQuery _query;
    private readonly ILogger<HistoryModel> _logger;

    public HistoryModel(OrderHistoryQuery query, ILogger<HistoryModel> logger)
    {
        _query = query;
        _logger = logger;
    }

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        try
        {
            var rows = await _query.ExecuteAsync(userId, cancellationToken);

            OrderGroups = rows
                .GroupBy(r => new { r.OrderId, r.OrderCreatedAt, r.OrderStatus })
                .Select(g => new OrderGroup(
                    g.Key.OrderId,
                    g.Key.OrderCreatedAt,
                    g.Key.OrderStatus,
                    g.Select(r => new OrderHistoryLineItem(
                        r.ProductId,
                        r.ProductName,
                        r.Price,
                        r.Quantity,
                        r.ReviewId,
                        r.ReviewTitle,
                        r.ReviewComment
                    )).ToList()
                ))
                .ToList();

            return Page();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Order history request was cancelled");
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load order history for user {UserId}", userId);
            TempData["ErrorMessage"] = "An error occurred while loading your order history. Please try again.";
            return Page();
        }
    }
}

public record OrderHistoryLineItem(
    Guid ProductId,
    string ProductName,
    decimal Price,
    int Quantity,
    Guid? ReviewId,
    string? ReviewTitle,
    string? ReviewComment
)
{
    public decimal LineTotal => Price * Quantity;
    public bool HasReview => ReviewId.HasValue;
}

public record OrderGroup(
    Guid OrderId,
    DateTimeOffset CreatedAt,
    OrderStatus Status,
    List<OrderHistoryLineItem> Items
)
{
    public decimal OrderTotal => Items.Sum(i => i.LineTotal);
}
