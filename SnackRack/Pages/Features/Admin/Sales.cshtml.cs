using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SnackRack.Data;
using SnackRack.Pages.Features.Orders;

namespace SnackRack.Pages.Features.Admin;

public class Sales : PageModel
{
    public List<OrderSummary> OrderSummaries { get; set; } = [];
    private readonly ILogger<Sales> _logger;
    private readonly ApplicationDbContext _context;

    public Sales(ILogger<Sales> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        try
        {
            var orders = await _context.Orders
                .Where(o => o.Status.Equals(OrderStatus.Submitted))
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync(cancellationToken);

            var productIds = orders
                .SelectMany(o => o.OrderItems
                    .Select(i => i.ProductId))
                .Distinct()
                .ToList();

            var reviews = await _context.Reviews
                .AsNoTracking()
                .Include(r => r.Product)
                .Include(r => r.User)
                .Where(r => productIds.Contains(r.Product.Id))
                .ToListAsync(cancellationToken);

            var reviewsByProductId = reviews
                .GroupBy(r => r.Product.Id)
                .ToDictionary(g => g.Key, g => g.ToList());

            OrderSummaries = orders.Select(o =>
            {
                var items = o.OrderItems
                    .Select(i => new OrderItemSummary(i.ProductName, i.Price, i.Quantity))
                    .ToList();

                var orderProductIds = o.OrderItems.
                    Select(i => i.ProductId)
                    .ToHashSet();
                var orderReviews = orderProductIds
                    .Where(pid => reviewsByProductId.ContainsKey(pid))
                    .SelectMany(pid => reviewsByProductId[pid])
                    .Select(r => new ReviewSummary(r.Product.Name, r.Title, r.Comment, r.User.Email ?? "Unknown"))
                    .ToList();

                return new OrderSummary(
                    o.Id,
                    o.Customer.Name,
                    o.Customer.Email,
                    o.CreatedAt,
                    o.Status,
                    items,
                    orderReviews
                );
            }).ToList();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting sales");
            TempData["ErrorMessage"] = "An error occurred while getting your sales. Please try again.";
            return Page();
        }
    }
}

public record OrderItemSummary(string ProductName, decimal Price, int Quantity)
{
    public decimal LineTotal => Price * Quantity;
}

public record ReviewSummary(string ProductName, string Title, string Comment, string UserEmail);

public record OrderSummary(
    Guid OrderId,
    string CustomerName,
    string? CustomerEmail,
    DateTimeOffset CreatedAt,
    OrderStatus Status,
    List<OrderItemSummary> Items,
    List<ReviewSummary> Reviews)
{
    public decimal OrderTotal => Items.Sum(i => i.LineTotal);
}
