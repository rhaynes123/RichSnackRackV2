using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SnackRack.Data;
using SnackRack.Pages.Features.Orders;

namespace SnackRack.Pages.Features.Admin;

public class Sales : PageModel
{
    private const int PageSize = 50;

    public List<OrderSummary> OrderSummaries { get; set; } = [];
    public int TotalOrderCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalOrderCount / (double)PageSize);

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

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
            TotalOrderCount = await _context.Orders.CountAsync(cancellationToken);

            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync(cancellationToken);

            var productIds = orders
                .SelectMany(o => o.OrderItems.Select(i => i.ProductId))
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

                var orderProductIds = o.OrderItems
                    .Select(i => i.ProductId)
                    .ToHashSet();

                var orderReviews = orderProductIds
                    .Where(pid => reviewsByProductId.ContainsKey(pid))
                    .SelectMany(pid => reviewsByProductId[pid])
                    .Select(r => new ReviewSummary(r.Product.Name, r.Title, r.Comment, r.User.Email ?? "Unknown"))
                    .ToList();

                return new OrderSummary(
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
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Sales page request was cancelled");
            return Page();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "A database error occurred while getting sales");
            TempData["ErrorMessage"] = "A database error occurred while getting your sales. Please try again.";
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while getting sales");
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
    string CustomerName,
    string? CustomerEmail,
    DateTimeOffset CreatedAt,
    OrderStatus Status,
    List<OrderItemSummary> Items,
    List<ReviewSummary> Reviews)
{
    public decimal OrderTotal => Items.Sum(i => i.LineTotal);
}
